import { Injectable, signal, computed, inject, DestroyRef } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OutboxService } from './outbox-service';
import { OutboxEntry } from './khataflow-db';
import { ApiResponse } from '../models/auth-models';
import { ProductUpdateRequest } from '../models/product-models';
import { CustomerUpdateRequest } from '../models/customer-models';

export type SyncState = 'idle' | 'syncing' | 'offline' | 'error';

// Kept low on purpose: without backend idempotency support, every retry is
// a real risk of creating a duplicate sale/expense. Better to fail fast and
// let the shopkeeper check manually than to hammer the API.
const MAX_RETRIES = 2;
const BASE_BACKOFF_MS = 2000;

// Wait this long after coming back online before syncing — a connection
// that just reconnected is often still flaky for a few seconds.
const RECONNECT_SETTLE_MS = 4000;

interface BuiltRequest {
  method: 'POST' | 'PUT';
  url: string;
}

@Injectable({ providedIn: 'root' })
export class SyncService {
  private readonly http = inject(HttpClient);
  private readonly outbox = inject(OutboxService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly _state = signal<SyncState>(navigator.onLine ? 'idle' : 'offline');
  readonly state = computed(() => this._state());

  private syncing = false;

  init(businessId: string): void {
    const onOnline = () => {
      setTimeout(() => void this.syncAll(businessId), RECONNECT_SETTLE_MS);
    };
    const onOffline = () => this._state.set('offline');

    window.addEventListener('online', onOnline);
    window.addEventListener('offline', onOffline);
    this.destroyRef.onDestroy(() => {
      window.removeEventListener('online', onOnline);
      window.removeEventListener('offline', onOffline);
    });

    if (navigator.onLine) {
      void this.syncAll(businessId);
    }
  }

  async syncAll(businessId: string): Promise<void> {
    if (this.syncing || !navigator.onLine) return;
    this.syncing = true;
    this._state.set('syncing');

    try {
      const pending = await this.outbox.getPending(businessId);

      for (const entry of pending) {
        if (entry.retryCount >= MAX_RETRIES) continue; // needs manual attention

        await this.syncOne(entry);
        await this.delay(150); // small stagger, don't burst-fire a queue of sales
      }

      this._state.set('idle');
    } finally {
      this.syncing = false;
    }
  }

  // Builds the real endpoint per operation. Update endpoints need the
  // entity's own id in the URL (your ProductService/CustomerService both
  // PUT to `${baseUrl}/${id}`) — a flat base path was wrong for those.
  private buildRequest(entry: OutboxEntry): BuiltRequest {
    const base = environment.apiUrl;

    switch (entry.type) {
      case 'CreateSale':
        return { method: 'POST', url: `${base}/api/v1/Sales` };
      case 'CreateExpense':
        return { method: 'POST', url: `${base}/api/v1/Expenses` };
      case 'UpdateProduct': {
        const p = entry.payload as ProductUpdateRequest;
        return { method: 'PUT', url: `${base}/api/v1/Products/${p.id}` };
      }
      case 'UpdateCustomer': {
        const c = entry.payload as CustomerUpdateRequest;
        return { method: 'PUT', url: `${base}/api/v1/Customers/${c.id}` };
      }
    }
  }

  private async syncOne(entry: OutboxEntry): Promise<void> {
    const { method, url } = this.buildRequest(entry);
    await this.outbox.markSyncing(entry.id);

    try {
      const res = await firstValueFrom(
        this.http.request<ApiResponse<unknown>>(method, url, { body: entry.payload })
      );

      // Your API returns 200 with `result: false` for expected business
      // failures (e.g. stock unavailable) rather than an HTTP error status —
      // checking only the HTTP status, like the first version of this file
      // did, would have silently marked these as synced. This is the fix.
      if (res.result) {
        await this.outbox.markSynced(entry.id);
      } else {
        await this.outbox.markConflict(entry.id, res.message ?? 'Request rejected by server');
      }
    } catch (err) {
      const httpErr = err as HttpErrorResponse;

      if (httpErr.status === 0) {
        // Genuinely lost connectivity mid-sync; stop the run, the `online`
        // listener restarts it once the network is back.
        this._state.set('offline');
        throw err;
      }

      // Real HTTP-level errors (401 expired token, 500, etc.) — worth a
      // bounded retry, unlike a business-rule rejection above.
      await this.outbox.markFailed(entry.id, httpErr.message);
      await this.delay(BASE_BACKOFF_MS * Math.pow(2, entry.retryCount));
    }
  }

  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}