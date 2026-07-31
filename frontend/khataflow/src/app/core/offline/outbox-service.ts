import { Injectable, signal, computed } from '@angular/core';
import { khataFlowDb, OutboxEntry, OutboxOperationType } from './khataflow-db';

@Injectable({ providedIn: 'root' })
export class OutboxService {
  private readonly _pendingCount = signal(0);
  readonly pendingCount = computed(() => this._pendingCount());

  constructor() {
    void this.refreshPendingCount();
  }

  async enqueue(
    id: string,
    type: OutboxOperationType,
    payload: OutboxEntry['payload'],
    businessId: string
  ): Promise<void> {
    const entry: OutboxEntry = {
      id,
      type,
      payload,
      businessId,
      createdAt: Date.now(),
      status: 'pending',
      retryCount: 0,
    };

    await khataFlowDb.outbox.put(entry);
    await this.refreshPendingCount();
  }

  async getPending(businessId: string): Promise<OutboxEntry[]> {
    return khataFlowDb.outbox
      .where('businessId')
      .equals(businessId)
      .filter((e) => e.status === 'pending' || e.status === 'failed')
      .sortBy('createdAt');
  }

  async markSyncing(id: string): Promise<void> {
    await khataFlowDb.outbox.update(id, { status: 'syncing' });
  }

  async markSynced(id: string): Promise<void> {
    await khataFlowDb.outbox.delete(id);
    await this.refreshPendingCount();
  }

  async markFailed(id: string, error: string): Promise<void> {
    const entry = await khataFlowDb.outbox.get(id);
    await khataFlowDb.outbox.update(id, {
      status: 'failed',
      lastError: error,
      retryCount: (entry?.retryCount ?? 0) + 1,
    });
  }

  async markConflict(id: string, error: string): Promise<void> {
    await khataFlowDb.outbox.update(id, { status: 'conflict', lastError: error });
    await this.refreshPendingCount();
  }

  
  async remove(id: string): Promise<boolean> {
    const entry = await khataFlowDb.outbox.get(id);
    if (!entry) return false;
    if (entry.status === 'syncing') return false;

    await khataFlowDb.outbox.delete(id);
    await this.refreshPendingCount();
    return true;
  }

  private async refreshPendingCount(): Promise<void> {
    const count = await khataFlowDb.outbox
      .filter((e) => e.status === 'pending' || e.status === 'failed')
      .count();
    this._pendingCount.set(count);
  }
}