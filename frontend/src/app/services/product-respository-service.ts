import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { khataFlowDb, LocalProduct } from '../core/offline/khataflow-db';
import { OutboxService } from '../core/offline/outbox-service';
import { SyncService } from '../core/offline/sync-service';
import { ApiResponse } from '../core/models/auth-models';
import { PaginatedResponse } from '../core/models/paginated-response-model';
import { Product, ProductUpdateRequest } from '../core/models/product-models';
import { Category } from '../core/models/category-models';

export interface QuickProductEdit {
  productId: string;
  price?: number;
  stock?: number;
}

// Thrown when a quick edit can't be safely built — surface this to the user
// rather than swallowing it, since sending a half-built ProductUpdateRequest
// would silently corrupt the product's category on the server.
export class CategoryNotResolvedError extends Error {
  constructor(productId: string) {
    super(
      `Can't edit product ${productId} offline yet — its category hasn't been ` +
        `resolved from the local cache. Open this product once while online first.`
    );
  }
}

@Injectable({ providedIn: 'root' })
export class ProductRepository {
  private readonly http = inject(HttpClient);
  private readonly outbox = inject(OutboxService);
  private readonly sync = inject(SyncService);
  private readonly productsUrl = `${environment.apiUrl}/api/v1/Products`;
  private readonly categoriesUrl = `${environment.apiUrl}/api/v1/Category`;

  private readonly _products = signal<LocalProduct[]>([]);
  readonly products = this._products.asReadonly();

  async loadForBusiness(businessId: string): Promise<void> {
    const local = await khataFlowDb.products.where('businessId').equals(businessId).sortBy('productName');
    this._products.set(local);
  }

  /**
   * Call while online (app start, pull-to-refresh). Pulls categories first,
   * then products — and resolves each product's categoryId by matching
   * categoryName against the category cache, since Product itself never
   * returns categoryId. If a product's category name doesn't match anything
   * in the category list (renamed/deleted category), categoryId is stored
   * as null and quick-edit will refuse until it's resolved online again.
   */
  async refreshFromServer(businessId: string, pageNumber = 1, pageSize = 100): Promise<void> {
    if (!navigator.onLine) return;

    const categoryRes = await firstValueFrom(
      this.http.get<ApiResponse<PaginatedResponse<Category>>>(this.categoriesUrl)
    );
    if (categoryRes.result) {
      const localCategories = categoryRes.data.items.map((c) => ({
        id: c.id,
        businessId,
        categoryName: c.categoryName,
      }));
      await khataFlowDb.categories.bulkPut(localCategories);
    }

    const nameToId = new Map(
      (await khataFlowDb.categories.where('businessId').equals(businessId).toArray()).map((c) => [
        c.categoryName,
        c.id,
      ])
    );

    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    const productRes = await firstValueFrom(
      this.http.get<ApiResponse<PaginatedResponse<Product>>>(this.productsUrl, { params })
    );
    if (!productRes.result) return;

    const localRows: LocalProduct[] = productRes.data.items.map((p) => ({
      id: p.id,
      businessId,
      productName: p.productName,
      categoryName: p.categoryName,
      categoryId: p.categoryName ? nameToId.get(p.categoryName) ?? null : null,
      price: p.price,
      stock: p.stock,
      inventoryStatus: p.inventoryStatus,
      updatedAt: Date.now(),
    }));

    await khataFlowDb.products.bulkPut(localRows);
    await this.loadForBusiness(businessId);
  }

  /**
   * Optimistic local price/stock edit. Builds a full ProductUpdateRequest
   * (it's a full-replace DTO) from the cached product plus the changed
   * fields. Throws CategoryNotResolvedError rather than guessing categoryId,
   * since a wrong id would silently move the product to another category.
   */
  async quickEdit(businessId: string, edit: QuickProductEdit): Promise<void> {
    const existing = await khataFlowDb.products.get(edit.productId);
    if (!existing) return;

    if (!existing.categoryId) {
      throw new CategoryNotResolvedError(edit.productId);
    }

    const newPrice = edit.price ?? existing.price;
    const newStock = edit.stock ?? existing.stock;

    const request: ProductUpdateRequest = {
      id: existing.id,
      productName: existing.productName,
      categoryId: existing.categoryId,
      price: newPrice,
      stock: newStock,
      inventoryStatus: existing.inventoryStatus,
    };

    const updated: LocalProduct = {
      ...existing,
      price: newPrice,
      stock: newStock,
      updatedAt: Date.now(),
      isDirty: true,
    };

    await khataFlowDb.products.put(updated);
    this._products.update((current) => current.map((p) => (p.id === edit.productId ? updated : p)));

    await this.outbox.enqueue(crypto.randomUUID(), 'UpdateProduct', request, businessId);
    void this.sync.syncAll(businessId);
  }
}