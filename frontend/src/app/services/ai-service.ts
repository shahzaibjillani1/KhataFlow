import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ReceiptParserRequest, TransactionAIResponse, VoiceCommandResponse } from '../core/models/ai-models';
import { ApiResponse } from '../core/models/auth-models';


@Injectable({
  providedIn: 'root',
})
export class AiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/AI`;

  readonly processing = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly lastVoiceResult = signal<VoiceCommandResponse | null>(null);
  readonly lastReceiptResult = signal<TransactionAIResponse | null>(null);

  sendVoiceCommand(audio: Blob | File): Observable<ApiResponse<VoiceCommandResponse>> {
  this.processing.set(true);
  this.error.set(null);

  const formData = new FormData();
  formData.append('audio', audio, audio instanceof File ? audio.name : 'recording.webm');

  return this.http.post<ApiResponse<VoiceCommandResponse>>(`${this.baseUrl}/voice-command`, formData).pipe(
    tap((res) => {
      this.processing.set(false);
      if (res.result && res.data?.success) {
        this.lastVoiceResult.set(res.data);
      } else {
        this.error.set(res.data?.errorMessage || res.message || 'Voice command failed');
      }
    }),
    catchError((err) => {
      this.processing.set(false);
      this.error.set('Failed to process voice command');
      return of({
        message: err.error?.message || err.message,
        result: false,
        data: null,
      } as unknown as ApiResponse<VoiceCommandResponse>);
    })
  );
}

  
  parseReceipt(text: string): Observable<TransactionAIResponse> {
    this.processing.set(true);
    this.error.set(null);

    const payload: ReceiptParserRequest = { text };

    return this.http.post<TransactionAIResponse>(`${this.baseUrl}/receipt-parser`, payload).pipe(
      tap((res) => {
        this.processing.set(false);
        if (res.success) {
          this.lastReceiptResult.set(res);
        } else {
          this.error.set(res.errorMessage || 'Receipt parsing failed');
        }
      }),
      catchError((err) => {
        this.processing.set(false);
        this.error.set('Failed to parse receipt');
        return of({
          success: false,
          errorMessage: err.message,
          transactionType: null,
          amount: null,
          currency: 'PKR',
          person: null,
          category: null,
          date: null,
          description: null,
        } as TransactionAIResponse);
      })
    );
  }

  reset(): void {
    this.lastVoiceResult.set(null);
    this.lastReceiptResult.set(null);
    this.error.set(null);
  }
}