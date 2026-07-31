import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/auth-models';
import { AddUdharRequest, CustomerKhata, RecordPaymentRequest } from '../core/models/ledger-models';

@Injectable({ providedIn: 'root' })
export class LedgerService {
  private http = inject(HttpClient);

  private baseUrl = `${environment.apiUrl}/api/v1/ledger`;

  getKhata(customerId: string) {
    return this.http.get<ApiResponse<CustomerKhata>>(this.baseUrl, {
      params: { customerId },
    });
  }

  addUdhar(customerId: string, amount: number, notes: string) {
    const request: AddUdharRequest = { customerId, amount, notes };
    return this.http.post<ApiResponse<CustomerKhata>>(
      `${this.baseUrl}/udhar`,
      request,
      { params: { customerId } }
    );
  }

  recordPayment(customerId: string, amount: number, notes: string) {
    const request: RecordPaymentRequest = { customerId, amount, notes };
    return this.http.post<ApiResponse<CustomerKhata>>(
      `${this.baseUrl}/payment`,
      request,
      { params: { customerId } }
    );
  }
}