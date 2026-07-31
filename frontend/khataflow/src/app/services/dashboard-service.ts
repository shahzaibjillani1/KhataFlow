import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { DashboardSummary } from './dashboard-models';
import { ApiResponse } from '../core/models/auth-models';


@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/Dashboard`;

  private readonly _summary = signal<DashboardSummary | null>(null);
  readonly summary = this._summary.asReadonly();

  fetchSummary() {
    return this.http
      .get<ApiResponse<DashboardSummary>>(`${this.baseUrl}/summary`)
      .pipe(tap((res) => this._summary.set(res.data)));
  }
}