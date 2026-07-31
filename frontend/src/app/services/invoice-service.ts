import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/Invoice`;

  getInvoicePdf(saleId: string) {
    return this.http.get(`${this.baseUrl}/${saleId}`, { responseType: 'blob' });
  }

  downloadInvoice(saleId: string, fileName?: string): void {
    this.getInvoicePdf(saleId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName ?? `invoice-${saleId}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Failed to download invoice', err),
    });
  }

  printInvoice(saleId: string): void {
    this.getInvoicePdf(saleId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);

        const iframe = document.createElement('iframe');
        iframe.style.position = 'fixed';
        iframe.style.right = '0';
        iframe.style.bottom = '0';
        iframe.style.width = '0';
        iframe.style.height = '0';
        iframe.style.border = '0';
        iframe.src = url;

        document.body.appendChild(iframe);

        iframe.onload = () => {
          try {
            iframe.contentWindow?.focus();
            iframe.contentWindow?.print();
          } catch (err) {
            console.error('Print failed, falling back to opening PDF', err);
            window.open(url, '_blank');
          }

          setTimeout(() => {
            document.body.removeChild(iframe);
            window.URL.revokeObjectURL(url);
          }, 60_000);
        };
      },
      error: (err) => console.error('Failed to load invoice for printing', err),
    });
  }
}