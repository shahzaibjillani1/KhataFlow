import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { TokenStorageService } from './token-storage-service';
import { environment } from '../../environments/environment.prod';
import {
  ApiResponse,
  AuthTokens,
  ForgotPasswordRequest,
  LoginRequest,
  RefreshRequest,
  RegisterRequest,
  ResetPasswordRequest,
} from '../core/models/auth-models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private tokenStorage = inject(TokenStorageService);


  private readonly baseUrl = `${environment.apiUrl}/api/v1/Auth`;

  register(request: RegisterRequest): Observable<ApiResponse<AuthTokens>> {
    return this.http
      .post<ApiResponse<AuthTokens>>(`${this.baseUrl}/register`, request)
      .pipe(tap((res) => this.tokenStorage.saveTokens(res.data)));
  }

  login(request: LoginRequest): Observable<ApiResponse<AuthTokens>> {
    return this.http
      .post<ApiResponse<AuthTokens>>(`${this.baseUrl}/login`, request)
      .pipe(tap((res) => this.tokenStorage.saveTokens(res.data)));
  }

  refresh(): Observable<ApiResponse<AuthTokens>> {
    const request: RefreshRequest = {
      accessToken: this.tokenStorage.getAccessToken() ?? '',
      refreshToken: this.tokenStorage.getRefreshToken() ?? '',
    };
    return this.http
      .post<ApiResponse<AuthTokens>>(`${this.baseUrl}/refresh`, request)
      .pipe(tap((res) => this.tokenStorage.saveTokens(res.data)));
  }

  getCurrentUserEmail(): string | null {
    return this.tokenStorage.currentUser()?.email ?? null;
  }

  logout(): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/logout`, {}).pipe(
      tap({
        next: () => this.tokenStorage.clear(),
        error: () => this.tokenStorage.clear(),
      }),
    );
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/reset-password`, request);
  }

  isLoggedIn(): boolean {
    return this.tokenStorage.isLoggedIn();
  }

  getRole(): string | null {
    return this.tokenStorage.getRole();
  }

  getCurrentUserId(): string | null {
    return this.tokenStorage.currentUser()?.sub ?? null;
  }

  getCurrentBusinessId(): string | null {
    return this.tokenStorage.currentUser()?.businessId ?? null;
  }
}
