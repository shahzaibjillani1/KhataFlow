import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule, NgForm } from '@angular/forms';
import {
  Subject,
  debounceTime,
  distinctUntilChanged,
  switchMap,
  forkJoin,
  of,
  catchError,
} from 'rxjs';
import { CustomerModal } from '../../../shared/components/customer-modal/customer-modal';
import { CustomerService } from '../../../services/customer-service';
import { LedgerService } from '../../../services/ledger-service';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { AiService } from '../../../services/ai-service';
import { CustomerKhata, LedgerEntry } from '../../../core/models/ledger-models';
import {
  Customer,
  CustomerAddRequest,
  CustomerUpdateRequest,
  PaginatedCustomerResponse,
} from '../../../core/models/customer-models';
import { VoiceIntent } from '../../../core/models/ai-models';
import { LocalizedTextPipe } from '../../../shared/pipes/localized-text-pipe';
import { LanguageService } from '../../../services/language-service';
import { environment } from '../../../../environments/environment.prod';
import { AuthService } from '../../../services/auth-service';
import { BusinessSubscriptionService } from '../../../services/business-subscription-service';

type RecordingState = 'idle' | 'recording' | 'processing';

interface CustomerFormModel {
  id?: string;
  name: string;
  phoneNumber: string;
  address: string;
  totalPurchases: number;
  udharAmount: number;
  lastVisit: string;
}

@Component({
  selector: 'app-customers',
  imports: [CommonModule, FormsModule, CustomerModal, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './customers.html',
  styleUrl: './customers.css',
})
export class Customers implements OnInit {
  private customerService = inject(CustomerService);
  private ledgerService = inject(LedgerService);
  private aiService = inject(AiService);
  private languageService = inject(LanguageService);
  private translocoService = inject(TranslocoService);
  private authService = inject(AuthService);
  private subscriptionService = inject(BusinessSubscriptionService);

  private readonly businessId = this.authService.getCurrentBusinessId() ?? '';

  readonly isPremium = this.subscriptionService.isPremium;
  private destroyRef = inject(DestroyRef);
  readonly isSearching = signal(false);
  lang = this.languageService.currentLang;

  search = '';
  selectedFilter = '';
  isModalOpen = false;
  isEditMode = false;
  isLoading = signal(true);

  customers = signal<Customer[]>([]);
  pageNumber = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalOutstanding = signal(0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  isSaving = signal(false);

  readonly role = this.authService.getRole();
  readonly isStaff = this.role === 'Staff';
  readonly canSeeOwnerOrManager = this.role === 'Owner' || this.role === 'Manager';

  showKhataModal = false;
  khataCustomer: Customer | null = null;
  khata = signal<CustomerKhata | null>(null);
  isKhataLoading = signal(false);

  showUdharModal = false;
  udharCustomer: Customer | null = null;
  udharForm = { amount: 0, note: '' };

  showPaymentModal = false;
  paymentCustomer: Customer | null = null;
  paymentForm = { amount: 0, note: '' };

  form: CustomerAddRequest & { id?: string } = this.emptyForm();

  private emptyForm() {
    return {
      name: '',
      phoneNumber: '',
      address: '',
      businessId: this.businessId,
      lastVisit: new Date().toISOString(),
      totalPurchases: 0,
      udharAmount: 0,
    };
  }

  selectedSort = '';
  sortCol = '';
  sortDir: 'asc' | 'desc' = 'asc';
  deleteConfirmId: string | null = null;

  selectedIds = new Set<string>();
  bulkDeleteConfirmOpen = signal(false);
  isDeleting = signal(false);

  private searchInput$ = new Subject<string>();

  readonly recordingState = signal<RecordingState>('idle');
  readonly voiceError = signal<string | null>(null);
  readonly voiceSuccessMessage = signal<string | null>(null);
  private mediaRecorder: MediaRecorder | null = null;
  private audioChunks: Blob[] = [];

  ngOnInit() {
    this.loadPage(1);

    this.searchInput$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) => {
          this.isSearching.set(true);
          return term.trim()
            ? this.customerService.search(term.trim(), 1, this.pageSize())
            : this.customerService.fetchAll(1, this.pageSize());
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.applyPage(res.data);
          this.isSearching.set(false);
        },
        error: (err) => {
          console.error('Search failed', err);
          this.isSearching.set(false);
        },
      });
  }

  onSearchChange(value: string) {
    this.search = value;
    this.searchInput$.next(value);
  }

  private loadPage(page: number) {
    this.isLoading.set(true);
    const request = this.search.trim()
      ? this.customerService.search(this.search.trim(), page, this.pageSize())
      : this.customerService.fetchAll(page, this.pageSize());

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.applyPage(res.data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load customers', err);
        this.isLoading.set(false);
      },
    });
  }

  private applyPage(data: PaginatedCustomerResponse) {
    this.customers.set(data.items);
    this.pageNumber.set(data.pageNumber);
    this.pageSize.set(data.pageSize);
    this.totalCount.set(data.totalCount);
    this.totalOutstanding.set(data.totalOutstanding);
    this.selectedIds = new Set();
  }

  goToPage(p: number) {
    if (p < 1 || p > this.totalPages() || p === this.pageNumber()) return;
    this.loadPage(p);
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages() }, (_, i) => i + 1);
  }

  setSortCol(col: string) {
    if (this.sortCol === col) this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    else {
      this.sortCol = col;
      this.sortDir = 'asc';
    }
    this.selectedSort = '';
  }

  sortIcon(col: string): string {
    if (this.sortCol !== col) return 'fa-sort text-text-muted';
    return this.sortDir === 'asc' ? 'fa-sort-up' : 'fa-sort-down';
  }

  get filteredCustomers(): Customer[] {
    return this.customers().filter((c) =>
      this.selectedFilter === 'udhar'
        ? c.udharAmount > 0
        : this.selectedFilter === 'clear'
          ? c.udharAmount === 0
          : true,
    );
  }

  get sortedCustomers(): Customer[] {
    const list = [...this.filteredCustomers];
    if (this.selectedSort) {
      switch (this.selectedSort) {
        case 'name_asc':
          return list.sort((a, b) => a.name.localeCompare(b.name));
        case 'name_desc':
          return list.sort((a, b) => b.name.localeCompare(a.name));
        case 'outstanding_desc':
          return list.sort((a, b) => b.udharAmount - a.udharAmount);
        case 'outstanding_asc':
          return list.sort((a, b) => a.udharAmount - b.udharAmount);
        case 'purchases_desc':
          return list.sort((a, b) => b.totalPurchases - a.totalPurchases);
        case 'lastvisit_desc':
          return list.sort(
            (a, b) => new Date(b.lastVisit).getTime() - new Date(a.lastVisit).getTime(),
          );
      }
    }
    if (this.sortCol) {
      return list.sort((a, b) => {
        let cmp = 0;
        if (this.sortCol === 'lastVisit')
          cmp = new Date(a.lastVisit).getTime() - new Date(b.lastVisit).getTime();
        else {
          const av = (a as any)[this.sortCol];
          const bv = (b as any)[this.sortCol];
          cmp = typeof av === 'string' ? av.localeCompare(bv) : av - bv;
        }
        return this.sortDir === 'asc' ? cmp : -cmp;
      });
    }
    return list;
  }

  get udharCustomers(): number {
    return this.customers().filter((c) => c.udharAmount > 0).length;
  }

  confirmDelete(id: string) {
    this.deleteConfirmId = id;
  }
  cancelDelete() {
    this.deleteConfirmId = null;
  }

  deleteCustomer(id: string) {
    this.customerService
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deleteConfirmId = null;
          this.selectedIds.delete(id);
          const nextPage =
            this.customers().length === 1 && this.pageNumber() > 1
              ? this.pageNumber() - 1
              : this.pageNumber();
          this.loadPage(nextPage);
        },
        error: (err) => console.error('Failed to delete customer', err),
      });
  }

  // ── Bulk selection ──────────────────────────────────────────────────
  get allSelected(): boolean {
    return (
      this.sortedCustomers.length > 0 &&
      this.sortedCustomers.every((c) => this.selectedIds.has(c.id))
    );
  }

  toggleSelectAll(checked: boolean) {
    if (checked) this.sortedCustomers.forEach((c) => this.selectedIds.add(c.id));
    else this.sortedCustomers.forEach((c) => this.selectedIds.delete(c.id));
    this.selectedIds = new Set(this.selectedIds);
  }

  toggleSelect(id: string) {
    this.selectedIds.has(id) ? this.selectedIds.delete(id) : this.selectedIds.add(id);
    this.selectedIds = new Set(this.selectedIds);
  }

  requestBulkDelete() {
    if (!this.selectedIds.size) return;
    this.bulkDeleteConfirmOpen.set(true);
  }

  cancelBulkDelete() {
    this.bulkDeleteConfirmOpen.set(false);
  }

  bulkDelete() {
    if (this.isDeleting() || !this.selectedIds.size) return;
    const ids = Array.from(this.selectedIds);

    this.isDeleting.set(true);
    forkJoin(
      ids.map((id) =>
        this.customerService.delete(id).pipe(
          catchError((err) => {
            console.error(`Failed to delete customer ${id}`, err);
            return of(null);
          }),
        ),
      ),
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const deletedCount = ids.length;
        this.selectedIds = new Set();
        this.bulkDeleteConfirmOpen.set(false);
        this.isDeleting.set(false);

        const nextPage =
          deletedCount >= this.customers().length && this.pageNumber() > 1
            ? this.pageNumber() - 1
            : this.pageNumber();
        this.loadPage(nextPage);
      });
  }

  openAddModal() {
    this.isModalOpen = true;
    this.isEditMode = false;
    this.form = this.emptyForm();
  }

  editCustomer(c: Customer) {
    this.isModalOpen = true;
    this.isEditMode = true;
    this.form = {
      id: c.id,
      name: c.name,
      phoneNumber: c.phoneNumber,
      address: c.address,
      businessId: this.businessId,
      lastVisit: c.lastVisit,
      totalPurchases: c.totalPurchases,
      udharAmount: c.udharAmount,
    };
  }

  saveCustomer(saved: CustomerFormModel) {
    if (this.isSaving()) return;
    this.isSaving.set(true);

    if (this.isEditMode && saved.id) {
      const request: CustomerUpdateRequest = {
        ...saved,
        id: saved.id,
        businessId: this.businessId,
      } as CustomerUpdateRequest;

      this.customerService
        .update(saved.id, request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.isSaving.set(false);
            this.isModalOpen = false;
            this.loadPage(this.pageNumber());
          },
          error: (err) => {
            console.error('Failed to update customer', err);
            this.isSaving.set(false);
          },
        });
    } else {
      const request: CustomerAddRequest = {
        ...saved,
        businessId: this.businessId,
      };

      this.customerService
        .add(request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.isSaving.set(false);
            this.isModalOpen = false;
            this.loadPage(1);
          },
          error: (err) => {
            console.error('Failed to add customer', err);
            this.isSaving.set(false);
          },
        });
    }
  }

  viewKhata(c: Customer) {
    this.khataCustomer = c;
    this.showKhataModal = true;
    this.isKhataLoading.set(true);
    this.ledgerService
      .getKhata(c.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.khata.set(res.data);
          this.isKhataLoading.set(false);
        },
        error: (err) => {
          console.error('Failed to load khata', err);
          this.isKhataLoading.set(false);
        },
      });
  }

  getKhataEntries(): LedgerEntry[] {
    return this.khata()?.entries ?? [];
  }
  get khataTotalPurchases(): number {
    return this.getKhataEntries()
      .filter((e) => e.type === 'Udhar')
      .reduce((sum, e) => sum + e.amount, 0);
  }
  get khataTotalPaid(): number {
    return this.getKhataEntries()
      .filter((e) => e.type === 'Cash')
      .reduce((sum, e) => sum + e.amount, 0);
  }

  addUdhar(c: Customer) {
    this.udharCustomer = c;
    this.udharForm = { amount: 0, note: '' };
    this.showUdharModal = true;
  }

  confirmAddUdhar() {
    if (!this.udharCustomer || !this.udharForm.amount) return;
    this.ledgerService
      .addUdhar(this.udharCustomer.id, this.udharForm.amount, this.udharForm.note)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.showUdharModal = false;
          this.loadPage(this.pageNumber());
          if (this.khataCustomer?.id === this.udharCustomer!.id)
            this.viewKhata(this.udharCustomer!);
        },
        error: (err) => console.error('Failed to add udhar', err),
      });
  }

  recordPayment(c: Customer) {
    this.paymentCustomer = c;
    this.paymentForm = { amount: 0, note: '' };
    this.showPaymentModal = true;
  }

  confirmPayment() {
    if (!this.paymentCustomer || !this.paymentForm.amount) return;
    this.ledgerService
      .recordPayment(this.paymentCustomer.id, this.paymentForm.amount, this.paymentForm.note)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.showPaymentModal = false;
          this.loadPage(this.pageNumber());
          if (this.khataCustomer?.id === this.paymentCustomer!.id)
            this.viewKhata(this.paymentCustomer!);
        },
        error: (err) => console.error('Failed to record payment', err),
      });
  }

  exportCsv() {
    const activeLang = this.lang();
    const localize = (en: string, ur: string | null | undefined) =>
      activeLang === 'ur' && ur?.trim() ? ur : en;

    const headers = ['Name', 'Phone', 'Address', 'Total Purchases', 'Outstanding', 'Last Visit'];
    const rows = this.customers().map((c) => [
      localize(c.name, c.nameUr),
      c.phoneNumber,
      localize(c.address ?? '', c.addressUr),
      c.totalPurchases,
      c.udharAmount,
      c.lastVisit,
    ]);
    const csv = [headers, ...rows].map((r) => r.join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'customers.csv';
    a.click();
    URL.revokeObjectURL(url);
  }

  viewAllUdhar() {
    this.selectedFilter = 'udhar';
    if (this.pageNumber() !== 1) {
      this.loadPage(1);
    }
    const el = document.querySelector('.customers-container') as HTMLElement | null;
    (el ?? window).scrollTo({ top: 0, behavior: 'smooth' });
  }

  get isRecording(): boolean {
    return this.recordingState() === 'recording';
  }
  get isProcessingVoice(): boolean {
    return this.recordingState() === 'processing';
  }

  async toggleRecording(): Promise<void> {
    if (this.isRecording) {
      this.stopRecording();
      return;
    }
    if (this.isProcessingVoice) return;

    this.voiceError.set(null);

    if (!navigator.mediaDevices?.getUserMedia) {
      this.voiceError.set('Microphone access is not supported in this browser.');
      return;
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.audioChunks = [];

      const mimeType = MediaRecorder.isTypeSupported('audio/webm') ? 'audio/webm' : '';
      this.mediaRecorder = mimeType
        ? new MediaRecorder(stream, { mimeType })
        : new MediaRecorder(stream);

      this.mediaRecorder.ondataavailable = (e) => {
        if (e.data.size > 0) this.audioChunks.push(e.data);
      };

      this.mediaRecorder.onstop = () => {
        stream.getTracks().forEach((track) => track.stop());
        const blob = new Blob(this.audioChunks, {
          type: this.mediaRecorder?.mimeType || 'audio/webm',
        });
        this.processVoiceRecording(blob);
      };

      this.mediaRecorder.start();
      this.recordingState.set('recording');
    } catch {
      this.voiceError.set('Microphone permission denied or unavailable.');
    }
  }

  private stopRecording(): void {
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      this.mediaRecorder.stop();
      this.recordingState.set('processing');
    }
  }

  private processVoiceRecording(audio: Blob): void {
    this.aiService
      .sendVoiceCommand(audio)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res) => {
        this.recordingState.set('idle');

        if (!res || !res.data) {
          this.voiceError.set(res?.message || 'Failed to process voice command.');
          return;
        }

        const voiceRes = res.data;
        if (!voiceRes.success) {
          this.voiceError.set(voiceRes.errorMessage || 'Could not understand the command.');
          return;
        }

        if (
          voiceRes.intent !== VoiceIntent.RecordPayment &&
          voiceRes.intent !== VoiceIntent.AddUdhar
        ) {
          this.voiceError.set(
            "That command was handled, but it wasn't a customer payment or udhar entry.",
          );
          return;
        }

        this.voiceSuccessMessage.set(voiceRes.message ?? 'Command completed.');
        this.loadPage(this.pageNumber());

        if (this.khataCustomer) {
          const stillOpenId = this.khataCustomer.id;
          this.customerService
            .fetchAll(this.pageNumber(), this.pageSize())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (fresh) => {
                const updated = fresh.data.items.find((c) => c.id === stillOpenId);
                if (updated) {
                  this.khataCustomer = updated;
                  this.viewKhata(updated);
                }
              },
            });
        }
      });
  }

  shareViaWhatsApp(customer: Customer) {
    const phone = this.toInternationalPhone(customer.phoneNumber);
    if (!phone) {
      console.error('Cannot share ledger — invalid phone number', customer.phoneNumber);
      return;
    }

    const link = `${environment.apiUrl}/ledger/${customer.publicToken}`;
    const displayName = this.lang() === 'ur' ? customer.nameUr || customer.name : customer.name;

    const message = this.translocoService.translate('customers.shareMessage', {
      name: displayName,
      link,
    });

    const whatsappUrl = `https://wa.me/${phone}?text=${encodeURIComponent(message)}`;
    window.open(whatsappUrl, '_blank', 'noopener,noreferrer');
  }

  private toInternationalPhone(raw: string | null | undefined): string | null {
    if (!raw) return null;
    const digits = raw.replace(/\D/g, '');
    if (/^92[3][0-9]{9}$/.test(digits)) return digits;
    if (/^0[3][0-9]{9}$/.test(digits)) return '92' + digits.slice(1);
    if (/^[3][0-9]{9}$/.test(digits)) return '92' + digits;
    return null;
  }
  
  clearSelection() {
  this.selectedIds = new Set();
}
}
