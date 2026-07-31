import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/auth-models';
import { Category, CategoryAddRequest, CategoryUpdateRequest } from '../core/models/category-models';
import { PaginatedResponse } from '../core/models/paginated-response-model';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/Category`;

  readonly _categories = signal<Category[]>([]);
  readonly categories = this._categories.asReadonly();

  fetchAll() {
    return this.http
      .get<ApiResponse<PaginatedResponse<Category>>>(this.baseUrl)
      .pipe(tap((res) => this._categories.set(res.data.items)));
  }

  getById(id: string) {
    return this.http.get<ApiResponse<Category>>(`${this.baseUrl}/${id}`);
  }

  add(request: CategoryAddRequest) {
    return this.http.post<ApiResponse<Category>>(this.baseUrl, request).pipe(
      tap((res) => this._categories.update((list) => [...list, res.data]))
    );
  }

  update(id: string, request: CategoryUpdateRequest) {
    return this.http.put<ApiResponse<Category>>(`${this.baseUrl}/${id}`, request).pipe(
      tap((res) => {
        this._categories.update((list) =>
          list.map((c) => (c.id === id ? res.data : c))
        );
      })
    );
  }

  delete(id: string) {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`).pipe(
      tap(() => {
        this._categories.update((list) => list.filter((c) => c.id !== id));
      })
    );
  }
}