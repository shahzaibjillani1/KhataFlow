import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { Location } from '@angular/common';
import { SaleService } from '../../../services/sale-service';
import { InvoiceService } from '../../../services/invoice-service';
import { InvoiceSettingsService } from '../../../services/invoice-settings';
import { BusinessSubscriptionService } from '../../../services/business-subscription-service';
import { Sale, SaleItemDetail } from '../../../core/models/sale-models';
import { PaymentStatus } from '../../../core/enums/payment-status';
import { InvoiceSettingsResponse } from '../../../core/models/invoice-settings-model';

interface InvoiceModel {
  id: string;
  invoiceNo: string;
  date: string;
  customer: string;
  itemCount: number;
  items: SaleItemDetail[];
  totalAmount: number;
  status: 'Paid' | 'Udhar' | 'Pending';
}

const PAYMENT_STATUS_LABEL: Record<PaymentStatus, InvoiceModel['status']> = {
  [PaymentStatus.Paid]: 'Paid',
  [PaymentStatus.Udhar]: 'Udhar',
  [PaymentStatus.Pending]: 'Pending',
};

@Component({
  selector: 'app-invoice',
  standalone: true,
  imports: [CommonModule, TranslocoDirective],
  templateUrl: './invoice.html',
  styleUrl: './invoice.css',
})
export class Invoice implements OnInit {
  private route = inject(ActivatedRoute);
  private saleService = inject(SaleService);
  private invoiceService = inject(InvoiceService);
  private invoiceSettingsService = inject(InvoiceSettingsService);
  private subscriptionService = inject(BusinessSubscriptionService);
  private location = inject(Location);
  private router = inject(Router);

  invoice = signal<InvoiceModel | null>(null);
  isLoading = signal(true);
  notFound = false;

  readonly isPremium = this.subscriptionService.isPremium;

  // Stays null for non-premium businesses, or if the settings fetch fails —
  // the template falls back to its default Tailwind classes whenever this is null.
  brandingStyle = signal<InvoiceSettingsResponse | null>(null);

  ngOnInit() {
    const saleId = this.route.snapshot.paramMap.get('id');

    if (!saleId) {
      this.notFound = true;
      this.isLoading.set(false);
      return;
    }

    this.loadInvoice(saleId);
    this.loadBranding();
  }

  private loadInvoice(saleId: string) {
    this.isLoading.set(true);
    this.notFound = false;

    this.saleService.getById(saleId).subscribe({
      next: (res) => {
        this.invoice.set(this.mapToInvoiceModel(res.data));
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load invoice', err);
        this.notFound = true;
        this.isLoading.set(false);
      },
    });
  }

  private async loadBranding(): Promise<void> {
    if (!this.isPremium()) return;

    try {
      const settings = await this.invoiceSettingsService.load();
      this.brandingStyle.set(settings);
    } catch (err) {
      // Branding is cosmetic — a failed fetch should never block the invoice
      // itself from rendering. Silently fall back to the default look.
      console.error('Failed to load invoice branding', err);
    }
  }

  private mapToInvoiceModel(sale: Sale): InvoiceModel {
    return {
      id: sale.id,
      invoiceNo: sale.invoiceNumber || `#${sale.id.slice(0, 8)}`,
      date: sale.date,
      customer: sale.customerName || 'Walk-in Customer',
      itemCount: sale.itemCount,
      items: sale.items ?? [],
      totalAmount: sale.totalAmount,
      status: PAYMENT_STATUS_LABEL[sale.paymentStatus] ?? 'Pending',
    };
  }

  get inv(): InvoiceModel {
    return this.invoice()!;
  }

  goBack() {
    this.location.back();
  }

  print(): void {
    this.invoiceService.printInvoice(this.inv.id);
  }

  shareWhatsApp(): void {
    const inv = this.inv;

    const formattedDate = new Date(inv.date).toLocaleString('en-PK', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });

    const formattedAmount = inv.totalAmount.toLocaleString('en-PK');
    const statusLine = `Status: ${inv.status}`;

    const itemLines = inv.items.length
      ? inv.items
          .map(
            (i) => `${i.productName} × ${i.quantity} — Rs ${i.lineTotal.toLocaleString('en-PK')}`,
          )
          .join('\n')
      : `${inv.itemCount} item(s)`;

    const msg = [
      `*Invoice ${inv.invoiceNo}*`,
      ``,
      `Customer: ${inv.customer}`,
      `Date: ${formattedDate}`,
      ``,
      itemLines,
      ``,
      statusLine,
      `*Total: Rs ${formattedAmount}*`,
      ``,
      `_Powered by KhataFlow_`,
    ].join('\n');

    const url = `https://wa.me/?text=${encodeURIComponent(msg)}`;
    window.open(url, '_blank');
  }

  downloadPDF(): void {
    this.invoiceService.downloadInvoice(this.inv.id, `invoice-${this.inv.invoiceNo}.pdf`);
  }

  statusKey(): string {
    return {
      Paid: 'invoice.status.paid',
      Udhar: 'invoice.status.udhar',
      Pending: 'invoice.status.pending',
    }[this.inv.status];
  }
}
