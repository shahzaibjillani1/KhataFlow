import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/auth-models';
import { User, UserUpdateRequest } from '../core/models/user-model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);

  private readonly baseUrl = `${environment.apiUrl}/api/v1/Users`;

  getUsers() {
    return this.http.get<ApiResponse<User[]>>(this.baseUrl);
  }

  updateUser(id: string, request: UserUpdateRequest) {
    return this.http.put<ApiResponse<User>>(`${this.baseUrl}/${id}`, request);
  }

  getUserById(id: string) {
    return this.http.get<ApiResponse<User>>(`${this.baseUrl}/${id}`);
  }
}