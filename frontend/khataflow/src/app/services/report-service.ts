import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/auth-models';
import { FinancialReport } from '../core/models/report-models';


@Injectable({ providedIn: 'root' })
export class ReportService {
  private http = inject(HttpClient);

  private readonly baseUrl = `${environment.apiUrl}/api/v1/Reports`;

  private dateRangeParams(from: string, to: string) {
    return new HttpParams()
      .set('from', from)
      .set('to', to)
  }

  getFinancialReport(from: string, to: string) {
    return this.http.get<ApiResponse<FinancialReport>>(`${this.baseUrl}/financial-report`, {
      params: this.dateRangeParams(from, to),
    });
  }

  getGrossProfit(from: string, to: string) {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/gross-profit`, {
      params: this.dateRangeParams(from, to),
    });
  }

  getTotalExpenses(from: string, to: string) {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/total-expenses`, {
      params: this.dateRangeParams(from, to),
    });
  }
}