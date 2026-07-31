import { Injectable, signal } from '@angular/core';
import { AuthTokens, DecodedToken } from '../core/models/auth-models';

const ACCESS_TOKEN_KEY = 'kf_access_token';
const REFRESH_TOKEN_KEY = 'kf_refresh_token';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  readonly currentUser = signal<DecodedToken | null>(this.decodeStoredToken());

  saveTokens(tokens: AuthTokens): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
    this.currentUser.set(this.decodeToken(tokens.accessToken));
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    this.currentUser.set(null);
  }

  isLoggedIn(): boolean {
    return !!this.getRefreshToken();
  }

  isAccessTokenExpired(): boolean {
    const token = this.getAccessToken();
    if (!token) return true;
    const decoded = this.decodeToken(token);
    if (!decoded) return true;
    return decoded.exp * 1000 <= Date.now();
  }

  getRole(): string | null {
    return this.currentUser()?.role ?? null;
  }

  private decodeStoredToken(): DecodedToken | null {
    const token = this.getAccessToken();
    return token ? this.decodeToken(token) : null;
  }

  private decodeToken(token: string): DecodedToken | null {
    try {
      const payload = token.split('.')[1];
      const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(json) as DecodedToken;
    } catch {
      return null;
    }
  }
}
