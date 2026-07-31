import { HttpClient, HttpContext, HttpContextToken } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { CustomerLedgerViewResponse } from '../core/models/customer-ledger-view-response';
import { ApiResponse } from '../core/models/auth-models';

export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

type LoadState = 'idle' | 'loading' | 'loaded' | 'not-found' | 'error';

@Injectable({
  providedIn: 'root',
})
export class CustomerLedgerViewService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/publicledger`;

  private readonly _state = signal<LoadState>('idle');
  private readonly _ledger = signal<CustomerLedgerViewResponse | null>(null);

  readonly state = this._state.asReadonly();
  readonly ledger = this._ledger.asReadonly();
  readonly isLoading = computed(() => this._state() === 'loading');
  readonly notFound = computed(() => this._state() === 'not-found');

  getByToken(token: string): Observable<CustomerLedgerViewResponse | null> {
    this._state.set('loading');
    this._ledger.set(null); 

    return this.http
      .get<ApiResponse<CustomerLedgerViewResponse>>(`${this.baseUrl}/${token}`, {
        context: new HttpContext().set(SKIP_AUTH, true),
      })
      .pipe(
        map((res) => res.data),
        tap((data) => {
          this._ledger.set(data);
          this._state.set('loaded');
        }),
        catchError((err) => {
          this._state.set(err.status === 404 ? 'not-found' : 'error');
          this._ledger.set(null);
          return of(null);
        }),
      );
  }
}