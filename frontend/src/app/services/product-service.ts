import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { PaginatedResponse } from '../core/models/paginated-response-model';
import { ApiResponse } from '../core/models/auth-models';
import { Product, ProductAddRequest, ProductUpdateRequest } from '../core/models/product-models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/Products`;

  private readonly _page = signal<PaginatedResponse<Product> | null>(null);
  readonly page = this._page.asReadonly();

  readonly products = computed(() => this._page()?.items ?? []);
  readonly pageNumber = computed(() => this._page()?.pageNumber ?? 1);
  readonly pageSize = computed(() => this._page()?.pageSize ?? 10);
  readonly totalCount = computed(() => this._page()?.totalCount ?? 0);
  readonly totalPages = computed(() => this._page()?.totalPages ?? 0);
  readonly hasNextPage = computed(() => this._page()?.hasNextPage ?? false);
  readonly hasPreviousPage = computed(() => this._page()?.hasPreviousPage ?? false);

  getPaged(pageNumber: number, pageSize: number) {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http
      .get<ApiResponse<PaginatedResponse<Product>>>(this.baseUrl, { params })
      .pipe(tap((res) => this._page.set(res.data)));
  }

  private refreshCurrentPage() {
    const current = this._page();
    const pageNumber = current?.pageNumber ?? 1;
    const pageSize = current?.pageSize ?? 10;
    this.getPaged(pageNumber, pageSize).subscribe({
      error: (err) => console.error('Failed to refresh products page', err),
    });
  }

  add(request: ProductAddRequest) {
    return this.http
      .post<ApiResponse<Product>>(this.baseUrl, request)
      .pipe(tap(() => this.refreshCurrentPage()));
  }

  update(id: string, request: ProductUpdateRequest) {
    return this.http
      .put<ApiResponse<Product>>(`${this.baseUrl}/${id}`, request)
      .pipe(tap(() => this.refreshCurrentPage()));
  }

  delete(id: string) {
    return this.http
      .delete<ApiResponse<null>>(`${this.baseUrl}/${id}`)
      .pipe(tap(() => this.refreshCurrentPage()));
  }

  getLowStockCount() {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/low-stock/count`);
  }

  getLowStock() {
    return this.http.get<ApiResponse<Product[]>>(`${this.baseUrl}/low-stock`);
  }

  getInStock() {
    return this.http.get<ApiResponse<Product[]>>(`${this.baseUrl}/in-stock`);
  }

  getOutOfStock() {
    return this.http.get<ApiResponse<Product[]>>(`${this.baseUrl}/out-of-stock`);
  }

  getTopSales() {
    return this.http.get<ApiResponse<Product[]>>(`${this.baseUrl}/top-sales`);
  }

  getByCategory(categoryId: string) {
    return this.http.get<ApiResponse<Product[]>>(`${this.baseUrl}/category/${categoryId}`);
  }

  getByName(productName: string) {
    return this.http.get<ApiResponse<Product[]>>(
      `${this.baseUrl}/${encodeURIComponent(productName)}`,
    );
  }
}