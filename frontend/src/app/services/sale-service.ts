import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal, computed } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/auth-models';
import { MonthlyRevenue, Sale, SaleAddRequest, WeeklySales } from '../core/models/sale-models';
import { PaginatedResponse } from '../core/models/paginated-response-model';

@Injectable({ providedIn: 'root' })
export class SaleService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/Sales`;


  private readonly _sales = signal<Sale[]>([]);
  readonly sales = this._sales.asReadonly();

  private readonly _pageNumber = signal(1);
  private readonly _pageSize = signal(20);
  private readonly _totalCount = signal(0);
  private readonly _totalPages = signal(0);

  readonly pageNumber = this._pageNumber.asReadonly();
  readonly pageSize = this._pageSize.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly totalPages = this._totalPages.asReadonly();
  readonly hasNextPage = computed(() => this._pageNumber() < this._totalPages());
  readonly hasPreviousPage = computed(() => this._pageNumber() > 1);

  fetchAll(pageNumber = 1, pageSize = 20) {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http
      .get<ApiResponse<PaginatedResponse<Sale>>>(this.baseUrl, { params })
      .pipe(
        tap((res) => {
          this._sales.set(res.data.items);
          this._pageNumber.set(res.data.pageNumber);
          this._pageSize.set(res.data.pageSize);
          this._totalCount.set(res.data.totalCount);
          this._totalPages.set(res.data.totalPages);
        })
      );
  }

  getById(id: string) {
    return this.http.get<ApiResponse<Sale>>(`${this.baseUrl}/${id}`);
  }

  add(request: SaleAddRequest) {
    return this.http.post<ApiResponse<Sale>>(this.baseUrl, request);
  }

  addBulk(requests: SaleAddRequest[]) {
    return this.http.post<ApiResponse<Sale[]>>(`${this.baseUrl}/bulk`, requests);
  }

  delete(id: string) {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }

  search(query: string) {
    const params = new HttpParams().set('query', query);
    return this.http.get<ApiResponse<Sale[]>>(`${this.baseUrl}/search`, { params });
  }

  getTodaySales() {
    return this.http.get<ApiResponse<Sale[]>>(`${this.baseUrl}/today-sales`);
  }

  getTotalMonthlyRevenue() {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/total-monthly-revenue`);
  }

  getTotalSales() {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/total-sales`);
  }

  getWeeklySales() {
    return this.http.get<ApiResponse<WeeklySales[]>>(`${this.baseUrl}/weekly-sales`);
  }

  getMonthlyRevenue(year: number) {
    const params = new HttpParams().set('year', year);
    return this.http.get<ApiResponse<MonthlyRevenue[]>>(`${this.baseUrl}/monthly-revenue`, { params });
  }
}