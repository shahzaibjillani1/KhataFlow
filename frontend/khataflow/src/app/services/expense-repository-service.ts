import { Injectable, inject, signal } from '@angular/core';
import { khataFlowDb, LocalExpense } from '../core/offline/khataflow-db';
import { OutboxService } from '../core/offline/outbox-service';
import { SyncService } from '../core/offline/sync-service';
import { ExpenseAddRequest } from '../core/models/expense-models';

@Injectable({ providedIn: 'root' })
export class ExpenseRepository {
  private readonly outbox = inject(OutboxService);
  private readonly sync = inject(SyncService);

  private readonly _expenses = signal<LocalExpense[]>([]);
  readonly expenses = this._expenses.asReadonly();

  async loadForBusiness(businessId: string): Promise<void> {
    const local = await khataFlowDb.expenses
      .where('businessId')
      .equals(businessId)
      .reverse()
      .sortBy('createdAt');

    this._expenses.set(local);
  }

  // No conflict handling — two expense entries never compete with each
  // other, so unlike sales there's nothing to reconcile on sync *content*.
  async createExpense(businessId: string, request: ExpenseAddRequest): Promise<string> {
    const id = crypto.randomUUID();

    const expense: LocalExpense = {
      id,
      businessId,
      request,
      createdAt: Date.now(),
      syncStatus: 'pending',
    };

    await khataFlowDb.expenses.put(expense);
    this._expenses.update((current) => [expense, ...current]);

    await this.outbox.enqueue(id, 'CreateExpense', request, businessId);
    void this.sync.syncAll(businessId);

    return id;
  }

  // Called after a sync run finishes (SyncService.state() === 'idle').
  // A synced expense's outbox entry is deleted by OutboxService.markSynced,
  // but the LocalExpense row itself is left behind here — this is the pass
  // that catches up: for each local expense still flagged 'pending', check
  // whether its outbox entry is gone (= synced) and, if so, drop the local
  // copy so it stops double-showing once ExpenseService's next server fetch
  // brings the same expense back through the normal (non-offline) list.
  async reconcile(businessId: string): Promise<void> {
    const local = await khataFlowDb.expenses
      .where('businessId')
      .equals(businessId)
      .toArray();

    const stillPending = local.filter((e) => e.syncStatus === 'pending');
    if (!stillPending.length) return;

    const toDrop: string[] = [];
    for (const expense of stillPending) {
      const outboxEntry = await khataFlowDb.outbox.get(expense.id);
      // No outbox entry left for this id means it either synced (removed by
      // markSynced) or was cancelled elsewhere — either way it shouldn't
      // still be shown as pending.
      if (!outboxEntry) toDrop.push(expense.id);
    }

    if (toDrop.length) {
      await khataFlowDb.expenses.bulkDelete(toDrop);
    }

    await this.loadForBusiness(businessId);
  }

  // Cancels a not-yet-synced expense: removes both the local record and its
  // outbox entry. Returns false (without deleting anything) if the entry is
  // already mid-sync — cancelling out from under an in-flight request would
  // leave the outbox and the local list disagreeing about whether it exists.
  async cancelPending(id: string): Promise<boolean> {
    const outboxEntry = await khataFlowDb.outbox.get(id);
    if (outboxEntry?.status === 'syncing') {
      return false;
    }

    await khataFlowDb.outbox.delete(id);
    await khataFlowDb.expenses.delete(id);
    this._expenses.update((current) => current.filter((e) => e.id !== id));

    return true;
  }
}