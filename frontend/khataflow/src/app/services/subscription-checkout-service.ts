import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/auth-models';

export interface CheckoutResponse {
  checkoutUrl: string;
}

@Injectable({ providedIn: 'root' })
export class SubscriptionCheckoutService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/SubscriptionCheckout`;

  startCheckout(planId: string): Observable<ApiResponse<CheckoutResponse>> {
    return this.http.post<ApiResponse<CheckoutResponse>>(`${this.baseUrl}/checkout`, { planId });
  }
}