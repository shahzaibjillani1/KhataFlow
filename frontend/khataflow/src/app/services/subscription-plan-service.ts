import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { SubscriptionPlan, SubscriptionPlanAddRequest, SubscriptionPlanUpdateRequest } from '../core/models/subscription-plan-models';
import { ApiResponse } from '../core/models/auth-models';


@Injectable({ providedIn: 'root' })
export class SubscriptionPlanService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/SubscriptionPlan`;

  private readonly _plans = signal<SubscriptionPlan[]>([]);
  readonly plans = this._plans.asReadonly();

  fetchAll() {
    return this.http
      .get<ApiResponse<SubscriptionPlan[]>>(this.baseUrl)
      .pipe(tap((res) => this._plans.set(res.data)));
  }

  getById(id: string) {
    return this.http.get<ApiResponse<SubscriptionPlan>>(`${this.baseUrl}/${id}`);
  }

  add(request: SubscriptionPlanAddRequest) {
    return this.http
      .post<ApiResponse<SubscriptionPlan>>(this.baseUrl, request)
      .pipe(
        tap((res) => {
          if (!res.result) {
            console.warn(
              '[SubscriptionPlanService] POST returned result:false despite apparent success — known backend bug, see comment.'
            );
          }
          this._plans.update((list) => [...list, res.data]);
        })
      );
  }

  update(id: string, request: SubscriptionPlanUpdateRequest) {
    return this.http.put<ApiResponse<SubscriptionPlan>>(`${this.baseUrl}/${id}`, request).pipe(
      tap((res) => {
        this._plans.update((list) => list.map((p) => (p.id === id ? res.data : p)));
      })
    );
  }

  delete(id: string) {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`).pipe(
      tap(() => {
        this._plans.update((list) => list.filter((p) => p.id !== id));
      })
    );
  }

  getUserCount(id: string) {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/${id}/user-count`);
  }

  getRevenue(id: string) {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/${id}/revenue`);
  }
}