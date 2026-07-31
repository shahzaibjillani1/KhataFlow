import Dexie, { Table } from 'dexie';
import { SaleAddRequest } from '../models/sale-models';
import { ExpenseAddRequest } from '../models/expense-models';
import { ProductUpdateRequest } from '../models/product-models';
import { CustomerUpdateRequest } from '../models/customer-models';

export interface LocalProduct {
  id: string; // server GUID
  businessId: string;
  productName: string;
  categoryName: string | null;
  categoryId: string | null; // resolved client-side by joining against the category
                              // cache — Product itself only returns categoryName,
                              // but ProductUpdateRequest needs categoryId. null means
                              // "not resolved yet" — quick-edit must refuse until it is.
  price: number;
  stock: number;
  inventoryStatus: number;
  updatedAt: number;
  isDirty?: boolean;
}

export interface LocalSale {
  id: string; // client GUID, local key only (no idempotency role — frontend-only)
  businessId: string;
  request: SaleAddRequest; // exact payload POSTed to /api/v1/Sales
  displayTotal: number; // computed client-side from cached product prices, for UI only
  createdAt: number;
  syncStatus: 'pending' | 'synced' | 'conflict';
  conflictMessage?: string;
}

export interface LocalExpense {
  id: string;
  businessId: string;
  request: ExpenseAddRequest; // exact payload POSTed to /api/v1/Expenses
  createdAt: number;
  syncStatus: 'pending' | 'synced';
}

// Local cache of categories, needed to resolve categoryId for product edits.
export interface LocalCategory {
  id: string;
  businessId: string;
  categoryName: string;
}

export type OutboxOperationType =
  | 'CreateSale'
  | 'UpdateProduct'
  | 'UpdateCustomer'
  | 'CreateExpense';

export interface OutboxEntry {
  id: string;
  type: OutboxOperationType;
  payload: SaleAddRequest | ProductUpdateRequest | CustomerUpdateRequest | ExpenseAddRequest;
  businessId: string;
  createdAt: number;
  status: 'pending' | 'syncing' | 'failed' | 'conflict';
  retryCount: number;
  lastError?: string;
}

export class KhataFlowDb extends Dexie {
  products!: Table<LocalProduct, string>;
  categories!: Table<LocalCategory, string>;
  sales!: Table<LocalSale, string>;
  expenses!: Table<LocalExpense, string>;
  outbox!: Table<OutboxEntry, string>;

  constructor() {
    super('khataflow-db');

    this.version(1).stores({
      products: 'id, businessId, updatedAt',
      sales: 'id, businessId, syncStatus, createdAt',
      outbox: 'id, businessId, status, createdAt',
    });

    this.version(2).stores({
      products: 'id, businessId, updatedAt',
      sales: 'id, businessId, syncStatus, createdAt',
      outbox: 'id, businessId, status, createdAt',
      expenses: 'id, businessId, syncStatus, createdAt',
    });

    // v3 adds categories, needed to resolve categoryId for offline product edits.
    this.version(3).stores({
      products: 'id, businessId, updatedAt',
      sales: 'id, businessId, syncStatus, createdAt',
      outbox: 'id, businessId, status, createdAt',
      expenses: 'id, businessId, syncStatus, createdAt',
      categories: 'id, businessId, categoryName',
    });
  }
}

export const khataFlowDb = new KhataFlowDb();