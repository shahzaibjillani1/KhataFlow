import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/auth-models';
import { PaginatedResponse } from '../core/models/paginated-response-model';
import {
  Business,
  BusinessAddRequest,
  BusinessUpdateRequest,
  ChangeSubscriptionRequest,
  PlatformSummary,
} from '../core/models/business-models';

@Injectable({ providedIn: 'root' })
export class BusinessService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/Business`;

  private readonly _businesses = signal<Business[]>([]);
  readonly businesses = this._businesses.asReadonly();

  private readonly _totalCount = signal(0);
  readonly totalCount = this._totalCount.asReadonly();

  fetchAll(pageNumber = 1, pageSize = 20) {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);

    return this.http.get<ApiResponse<PaginatedResponse<Business>>>(this.baseUrl, { params }).pipe(
      tap((res) => {
        this._businesses.set(res.data.items);
        this._totalCount.set(res.data.totalCount);
      }),
    );
  }

  getById(id: string) {
    return this.http.get<ApiResponse<Business>>(`${this.baseUrl}/${id}`);
  }

  add(request: BusinessAddRequest) {
    return this.http.post<ApiResponse<Business>>(this.baseUrl, request);
  }

  update(id: string, request: BusinessUpdateRequest) {
    return this.http.put<ApiResponse<Business>>(`${this.baseUrl}/${id}`, request);
  }

  getPlatformSummary() {
    return this.http.get<ApiResponse<PlatformSummary>>(`${this.baseUrl}/platform-summary`);
  }

  suspend(id: string, reason: string) {
    const params = new HttpParams().set('reason', reason);
    return this.http.patch<ApiResponse<Business>>(`${this.baseUrl}/${id}/suspend`, null, {
      params,
    });
  }

  reactivate(id: string) {
    return this.http.patch<ApiResponse<Business>>(`${this.baseUrl}/${id}/reactivate`, null);
  }

  renewSubscription(id: string) {
    return this.http.post<ApiResponse<Business>>(`${this.baseUrl}/${id}/renew-subscription`, null);
  }

  changeSubscription(id: string, request: ChangeSubscriptionRequest) {
    return this.http.post<ApiResponse<Business>>(
      `${this.baseUrl}/${id}/change-subscription`,
      request,
    );
  }

  getImpersonationToken(id: string) {
    return this.http.post<ApiResponse<{ accessToken: string }>>(
      `${this.baseUrl}/${id}/impersonation-token`,
      null,
    );
  }
}
