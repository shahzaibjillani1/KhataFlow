export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  phoneNumber: string;
  businessName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshRequest {
  accessToken: string;
  refreshToken: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
  refreshTokenExpiry: string;
}

export interface ApiResponse<T> {
  message: string;
  result: boolean;
  data: T;
}

export interface DecodedToken {
  sub: string;             
  email: string;
  role: string;
  businessId: string;
  exp: number;
  [key: string]: unknown;
}