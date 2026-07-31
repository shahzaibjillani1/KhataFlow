import { Injectable, inject, signal } from '@angular/core';
import { khataFlowDb, LocalSale } from '../core/offline/khataflow-db';
import { OutboxService } from '../core/offline/outbox-service';
import { SyncService } from '../core/offline/sync-service';
import { SaleAddRequest, SaleItemRequest } from '../core/models/sale-models';

@Injectable({ providedIn: 'root' })
export class SalesRepository {
  private readonly outbox = inject(OutboxService);
  private readonly sync = inject(SyncService);

  private readonly _sales = signal<LocalSale[]>([]);
  readonly sales = this._sales.asReadonly();

  async loadForBusiness(businessId: string): Promise<void> {
    const local = await khataFlowDb.sales
      .where('businessId')
      .equals(businessId)
      .reverse()
      .sortBy('createdAt');

    this._sales.set(local);
  }

  async createSale(
    businessId: string,
    request: SaleAddRequest,
    displayTotal: number,
  ): Promise<string> {
    const id = crypto.randomUUID();

    const sale: LocalSale = {
      id,
      businessId,
      request,
      displayTotal,
      createdAt: Date.now(),
      syncStatus: 'pending',
    };

    await khataFlowDb.sales.put(sale);
    this._sales.update((current) => [sale, ...current]);

    await this.outbox.enqueue(id, 'CreateSale', request, businessId);
    void this.sync.syncAll(businessId);

    return id;
  }

  async markConflict(saleId: string, message: string): Promise<void> {
    await khataFlowDb.sales.update(saleId, { syncStatus: 'conflict', conflictMessage: message });
    this._sales.update((current) =>
      current.map((s) =>
        s.id === saleId ? { ...s, syncStatus: 'conflict', conflictMessage: message } : s,
      ),
    );
  }

  async reconcile(businessId: string): Promise<void> {
    const synced = await khataFlowDb.sales
      .where('businessId')
      .equals(businessId)
      .filter((s) => s.syncStatus === 'synced')
      .toArray();

    if (synced.length) {
      await khataFlowDb.sales.bulkDelete(synced.map((s) => s.id));
    }

    await this.loadForBusiness(businessId);
  }

  async cancelPending(id: string): Promise<boolean> {
    const sale = this._sales().find((s) => s.id === id);
    if (!sale || sale.syncStatus !== 'pending') return false;

    const removedFromOutbox = await this.outbox.remove(id);
    if (!removedFromOutbox) return false;

    await khataFlowDb.sales.delete(id);
    this._sales.update((current) => current.filter((s) => s.id !== id));

    return true;
  }
}
