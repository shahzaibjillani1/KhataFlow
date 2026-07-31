import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { Expense, ExpenseAddRequest, ExpenseByCategory } from '../core/models/expense-models';
import { ApiResponse } from '../core/models/auth-models';
import { PaginatedResponse } from '../core/models/paginated-response-model';

@Injectable({
  providedIn: 'root',
})
export class ExpenseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/Expenses`;

  private readonly _page = signal<PaginatedResponse<Expense> | null>(null);
  readonly page = this._page.asReadonly();


  readonly expenses = computed(() => this._page()?.items ?? []);
  readonly pageNumber = computed(() => this._page()?.pageNumber ?? 1);
  readonly pageSize = computed(() => this._page()?.pageSize ?? 20);
  readonly totalCount = computed(() => this._page()?.totalCount ?? 0);
  readonly totalPages = computed(() => this._page()?.totalPages ?? 0);
  readonly hasNextPage = computed(() => this._page()?.hasNextPage ?? false);
  readonly hasPreviousPage = computed(() => this._page()?.hasPreviousPage ?? false);

  readonly totalExpense = signal<number>(0);
  readonly expensesByCategory = signal<ExpenseByCategory[]>([]);
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  getAll(pageNumber = 1, pageSize = 20): Observable<ApiResponse<PaginatedResponse<Expense>>> {
    this.loading.set(true);
    this.error.set(null);

    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);

    return this.http.get<ApiResponse<PaginatedResponse<Expense>>>(this.baseUrl, { params }).pipe(
      tap((res) => {
        if (res.result) {
          this._page.set(res.data);
        }
        this.loading.set(false);
      }),
      catchError((err) => {
        this.error.set('Failed to load expenses');
        this.loading.set(false);
        return of({
          message: err.message,
          result: false,
          data: {
            items: [],
            pageNumber,
            pageSize,
            totalCount: 0,
            totalPages: 0,
            hasNextPage: false,
            hasPreviousPage: false,
          },
        } as ApiResponse<PaginatedResponse<Expense>>);
      }),
    );
  }

  private refreshCurrentPage() {
    const current = this._page();
    this.getAll(current?.pageNumber ?? 1, current?.pageSize ?? 20).subscribe({
      error: (err) => console.error('Failed to refresh expenses page', err),
    });
  }

  add(request: ExpenseAddRequest): Observable<ApiResponse<Expense>> {
    return this.http.post<ApiResponse<Expense>>(this.baseUrl, request).pipe(
      tap((res) => {
        if (res.result) {
          this.refreshCurrentPage();
        }
      }),
      catchError((err) => {
        this.error.set('Failed to add expense');
        return of({
          message: err.message,
          result: false,
          data: null,
        } as unknown as ApiResponse<Expense>);
      }),
    );
  }

  delete(id: string): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/${id}`).pipe(
      tap((res) => {
        if (res.result) {
          this.refreshCurrentPage();
        }
      }),
      catchError((err) => {
        this.error.set('Failed to delete expense');
        return of({ message: err.message, result: false, data: false } as ApiResponse<boolean>);
      }),
    );
  }

  getTotal(from: string, to: string): Observable<ApiResponse<number>> {
    return this.http
      .get<ApiResponse<number>>(`${this.baseUrl}/total`, { params: { from, to } })
      .pipe(
        tap((res) => {
          if (res.result) {
            this.totalExpense.set(res.data);
          }
        }),
        catchError((err) => {
          this.error.set('Failed to load total expenses');
          return of({ message: err.message, result: false, data: 0 } as ApiResponse<number>);
        }),
      );
  }

  getByCategory(from: string, to: string): Observable<ApiResponse<ExpenseByCategory[]>> {
    return this.http
      .get<ApiResponse<ExpenseByCategory[]>>(`${this.baseUrl}/by-category`, {
        params: { from, to },
      })
      .pipe(
        tap((res) => {
          if (res.result) {
            this.expensesByCategory.set(res.data);
          }
        }),
        catchError((err) => {
          this.error.set('Failed to load expense breakdown');
          return of({ message: err.message, result: false, data: [] } as ApiResponse<
            ExpenseByCategory[]
          >);
        }),
      );
  }
}
