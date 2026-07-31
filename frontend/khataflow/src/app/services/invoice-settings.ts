import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment.prod';
import {
  InvoiceSettingsRequest,
  InvoiceSettingsResponse,
} from '../core/models/invoice-settings-model';
import { ApiResponse } from '../core/models/auth-models';

@Injectable({
  providedIn: 'root',
})
export class InvoiceSettingsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/InvoiceSettings`;

  readonly settings = signal<InvoiceSettingsResponse | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);

  async load(): Promise<InvoiceSettingsResponse> {
    this.loading.set(true);
    try {
      const res = await firstValueFrom(
        this.http.get<ApiResponse<InvoiceSettingsResponse>>(this.baseUrl),
      );
      this.settings.set(res.data);
      return res.data;
    } finally {
      this.loading.set(false);
    }
  }

  async update(request: InvoiceSettingsRequest): Promise<InvoiceSettingsResponse> {
    this.saving.set(true);
    try {
      const res = await firstValueFrom(
        this.http.put<ApiResponse<InvoiceSettingsResponse>>(this.baseUrl, request),
      );
      this.settings.set(res.data);
      return res.data;
    } finally {
      this.saving.set(false);
    }
  }

  async preview(request: InvoiceSettingsRequest): Promise<Blob> {
    return firstValueFrom(
      this.http.post(`${this.baseUrl}/preview`, request, { responseType: 'blob' }),
    );
  }
}
