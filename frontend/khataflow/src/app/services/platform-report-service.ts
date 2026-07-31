import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { PlatformReportResponse } from '../core/models/platform-report-models';
import { ReportPeriod } from '../core/enums/report-period';
import { ApiResponse } from '../core/models/auth-models';


@Injectable({
  providedIn: 'root',
})
export class PlatformReportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/PlatformReport`;

  private readonly _report = signal<PlatformReportResponse | null>(null);
  private readonly _loading = signal<boolean>(false);
  private readonly _error = signal<string | null>(null);

  readonly report = this._report.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly growth = computed(() => this._report()?.growth ?? null);
  readonly revenueByPlan = computed(() => this._report()?.revenueByPlan ?? []);
  readonly topBusinesses = computed(() => this._report()?.topBusinesses ?? []);
  readonly recentActivity = computed(() => this._report()?.recentActivity ?? []);

  loadPlatformReport(period: ReportPeriod = ReportPeriod.Month): void {
    this._loading.set(true);
    this._error.set(null);

    this.getPlatformReport(period)
      .pipe(
        tap((data) => this._report.set(data)),
        catchError((err) => {
          this._error.set('Failed to load platform report.');
          console.error('PlatformReportService.loadPlatformReport error:', err);
          return of(null);
        }),
        finalize(() => this._loading.set(false))
      )
      .subscribe();
  }

  
  getPlatformReport(period: ReportPeriod = ReportPeriod.Month): Observable<PlatformReportResponse> {
    const params = new HttpParams().set('period', period.toString());

    return this.http.get<ApiResponse<PlatformReportResponse>>(this.baseUrl, { params }).pipe(
      map((res) => {
        if (!res.result) {
          throw new Error(res.message || 'Platform report request failed.');
        }
        return res.data;
      })
    );
  }
}