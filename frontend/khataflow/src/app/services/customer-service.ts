import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Customer,
  CustomerAddRequest,
  CustomerUpdateRequest,
  PaginatedCustomerResponse,
} from '../core/models/customer-models';
import { ApiResponse } from '../core/models/auth-models';

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private http = inject(HttpClient);

  private readonly baseUrl = `${environment.apiUrl}/api/v1/Customers`;

  private readonly _list = signal<PaginatedCustomerResponse | null>(null);
  readonly list = this._list.asReadonly();

  fetchAll(pageNumber = 1, pageSize = 20) {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http
      .get<ApiResponse<PaginatedCustomerResponse>>(this.baseUrl, { params })
      .pipe(tap((res) => this._list.set(res.data)));
  }

  getById(id: string) {
    return this.http.get<ApiResponse<Customer>>(`${this.baseUrl}/${id}`);
  }

  add(request: CustomerAddRequest) {
    return this.http.post<ApiResponse<Customer>>(this.baseUrl, request);
  }

  update(id: string, request: CustomerUpdateRequest) {
    return this.http.put<ApiResponse<Customer>>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string) {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }

  search(name: string, pageNumber = 1, pageSize = 20) {
    const params = new HttpParams()
      .set('name', name)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http.get<ApiResponse<PaginatedCustomerResponse>>(
      `${this.baseUrl}/search`,
      { params },
    );
  }
}