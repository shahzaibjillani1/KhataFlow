import { Component, computed, DestroyRef, effect, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { ProductService } from '../../../services/product-service';
import { SaleService } from '../../../services/sale-service';
import { CustomerService } from '../../../services/customer-service';
import { AiService } from '../../../services/ai-service';
import { AuthService } from '../../../services/auth-service';
import { Product } from '../../../core/models/product-models';
import { SaleAddRequest, SaleItemRequest } from '../../../core/models/sale-models';
import { VoiceIntent } from '../../../core/models/ai-models';
import { LocalizedTextPipe } from '../../../shared/pipes/localized-text-pipe';
import { LanguageService } from '../../../services/language-service';
import {
  debounceTime,
  distinctUntilChanged,
  of,
  Subject,
  switchMap,
  forkJoin,
  catchError,
} from 'rxjs';

import { SalesRepository } from '../../../services/sales-repository-service';
import { SyncService } from '../../../core/offline/sync-service';
import { LocalProduct, LocalSale } from '../../../core/offline/khataflow-db';
import { ProductRepository } from '../../../services/product-respository-service';
import { BusinessSubscriptionService } from '../../../services/business-subscription-service';

interface CartItem {
  productId: string;
  name: string;
  nameUr: string | null;
  price: number;
  quantity: number;
}

interface CustomerOption {
  id: string;
  name: string;
  nameUr: string | null;
  phone: string;
  outstanding: number;
}

const PAYMENT_STATUS = {
  Paid: 0,
  Udhar: 1,
  Pending: 2,
} as const;

type PaymentStatusLabel = 'Paid' | 'Udhar' | 'Pending';
type RecordingState = 'idle' | 'recording' | 'processing';

const STATUS_LABEL: Record<number, PaymentStatusLabel> = {
  [PAYMENT_STATUS.Paid]: 'Paid',
  [PAYMENT_STATUS.Udhar]: 'Udhar',
  [PAYMENT_STATUS.Pending]: 'Pending',
};

const STATUS_KEY: Record<PaymentStatusLabel, string> = {
  Paid: 'sales.status.paid',
  Udhar: 'sales.status.udhar',
  Pending: 'sales.status.pending',
};

const STATUS_CLASS: Record<PaymentStatusLabel, string> = {
  Paid: 'bg-status-success-bg text-status-success-text border-status-success-border',
  Udhar: 'bg-udhar-bg text-udhar-value border-udhar-border',
  Pending: 'bg-danger-soft text-text-danger border-danger-border',
};

@Component({
  selector: 'app-sales',
  imports: [CommonModule, FormsModule, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './sales.html',
  styleUrl: './sales.css',
})
export class Sales implements OnInit {
  router = inject(Router);
  private productService = inject(ProductService);
  private saleService = inject(SaleService);
  private customerService = inject(CustomerService);
  private aiService = inject(AiService);
  private languageService = inject(LanguageService);
  private transloco = inject(TranslocoService);
  private destroyRef = inject(DestroyRef);
  private authService = inject(AuthService);
  private subscriptionService = inject(BusinessSubscriptionService);

  readonly isPremium = this.subscriptionService.isPremium;

  private readonly salesRepository = inject(SalesRepository);
  private readonly productRepository = inject(ProductRepository);
  private readonly syncService = inject(SyncService);
  private readonly businessId = signal(this.authService.getCurrentBusinessId() ?? '');
  readonly isOnline = signal(navigator.onLine);
  readonly offlineNotice = signal<string | null>(null);

  readonly pendingSales = computed(() =>
    this.salesRepository.sales().filter((s) => s.syncStatus === 'pending'),
  );
  readonly conflictSales = computed(() =>
    this.salesRepository.sales().filter((s) => s.syncStatus === 'conflict'),
  );
  readonly cancellingSaleId = signal<string | null>(null);

  lang = this.languageService.currentLang;

  search = '';
  selectedStatus = '';
  quantity = 1;
  discount = 0;
  selectedCustomerId = '';
  paymentMethod: 'cash' | 'card' | 'udhar' = 'cash';
  filterDate = '';
  methods: ('cash' | 'card' | 'udhar')[] = ['cash', 'card', 'udhar'];
  selectedCategory = '';

  showCheckoutModal = false;
  showInvoiceModal = false;
  showProductPicker = false;

  isLoading = signal(true);
  loadError = signal<string | null>(null);
  isSubmitting = signal(false);
  isDeleting = signal(false);
  deleteConfirmId = signal<string | null>(null);

  selectedIds = new Set<string>();
  bulkDeleteConfirmOpen = signal(false);

  cart: CartItem[] = [];

  private readonly serverProducts = signal<Product[]>([]);

  readonly products = computed<Product[]>(() => {
    if (this.isOnline()) return this.serverProducts();

    return this.productRepository.products().map((p: LocalProduct) => ({
      id: p.id,
      productName: p.productName,
      productNameUr: null,
      categoryName: p.categoryName,
      categoryNameUr: null,
      price: p.price,
      stock: p.stock,
      inventoryStatus: p.inventoryStatus,
    }));
  });

  customers = signal<CustomerOption[]>([]);
  private customerSearchTerm = signal('');
  private customerSearchResults = signal<CustomerOption[] | null>(null);
  isSearchingCustomers = signal(false);
  customerSearch$ = new Subject<string>();
  customerOptions = computed(() => this.customerSearchResults() ?? this.customers());

  cartPickerOpen = signal(false);
  checkoutPickerOpen = signal(false);

  salesHistory = this.saleService.sales;
  hasNextPage = this.saleService.hasNextPage;
  hasPreviousPage = this.saleService.hasPreviousPage;
  pageNumber = this.saleService.pageNumber;
  totalCount = this.saleService.totalCount;
  totalPages = this.saleService.totalPages;

  readonly recordingState = signal<RecordingState>('idle');
  readonly voiceError = signal<string | null>(null);
  readonly voiceSuccessMessage = signal<string | null>(null);
  private mediaRecorder: MediaRecorder | null = null;
  private audioChunks: Blob[] = [];

  todaySalesTotal = signal(0);
  todaySalesCount = signal(0);
  udharTotal = signal(0);

  constructor() {
    window.addEventListener('online', () => this.isOnline.set(true));
    window.addEventListener('offline', () => this.isOnline.set(false));

    effect(() => {
      if (this.syncService.state() === 'idle' && this.isOnline()) {
        void this.salesRepository.reconcile(this.businessId());
        this.refreshSalesData();
      }
    });
  }

  ngOnInit(): void {
    void this.salesRepository.loadForBusiness(this.businessId());
    void this.productRepository.loadForBusiness(this.businessId());

    this.loadAll();
    this.loadTodayStats();

    this.customerSearch$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) => {
          if (term.trim().length < 2) {
            this.customerSearchResults.set(null);
            return of(null);
          }
          if (!navigator.onLine) {
            this.customerSearchResults.set(null);
            return of(null);
          }
          this.isSearchingCustomers.set(true);
          return this.customerService.search(term.trim(), 1, 20);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.isSearchingCustomers.set(false);
          if (!res) return;
          this.customerSearchResults.set(
            res.data.items.map((c) => ({
              id: c.id,
              name: c.name,
              nameUr: c.nameUr ?? null,
              phone: c.phoneNumber,
              outstanding: c.udharAmount,
            })),
          );
        },
        error: (err) => {
          console.error('Customer search failed', err);
          this.isSearchingCustomers.set(false);
        },
      });
  }

  clearSelectedCustomer(): void {
    this.selectedCustomerId = '';
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }

  private loadAll(): void {
    if (!navigator.onLine) {
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.loadError.set(null);

    this.productService
      .getPaged(1, 100)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.serverProducts.set(res.data.items);
          void this.productRepository.refreshFromServer(this.businessId());
        },
        error: (err) => {
          console.error('Failed to load products', err);
          this.loadError.set(this.t('sales.errors.loadProducts'));
        },
      });

    this.loadCustomers();

    this.saleService
      .fetchAll(1, 20)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.isLoading.set(false),
        error: (err) => {
          console.error('Failed to load sales', err);
          this.loadError.set(this.t('sales.errors.loadSales'));
          this.isLoading.set(false);
        },
      });
  }

  private loadCustomers(): void {
    if (!navigator.onLine) return; 

    this.customerService
      .fetchAll(1, 100)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) =>
          this.customers.set(
            res.data.items.map((c) => ({
              id: c.id,
              name: c.name,
              nameUr: c.nameUr ?? null,
              phone: c.phoneNumber,
              outstanding: c.udharAmount,
            })),
          ),
        error: (err) => console.error('Failed to load customers', err),
      });
  }

  private loadTodayStats(): void {
    if (!navigator.onLine) return; 

    this.saleService
      .getTodaySales()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.todaySalesCount.set(res.data.length);
          this.todaySalesTotal.set(res.data.reduce((sum, s) => sum + s.totalAmount, 0));
          this.udharTotal.set(
            res.data
              .filter((s) => STATUS_LABEL[s.paymentStatus] === 'Udhar')
              .reduce((sum, s) => sum + s.totalAmount, 0),
          );
        },
        error: (err) => console.error('Failed to load today sales', err),
      });
  }

  private refreshSalesData(): void {
    if (!navigator.onLine) return;

    this.saleService
      .fetchAll(this.saleService.pageNumber(), this.saleService.pageSize())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (err) => console.error('Failed to refresh sales', err),
      });
    this.loadCustomers();
    this.loadTodayStats();
  }

  goToPage(page: number): void {
    if (page < 1 || !navigator.onLine) return;
    this.saleService
      .fetchAll(page, this.saleService.pageSize())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (err) => console.error('Failed to load page', err),
      });
  }

  paymentStatusLabel(status: number): PaymentStatusLabel {
    return STATUS_LABEL[status] ?? 'Paid';
  }

  paymentStatusKey(status: number): string {
    return STATUS_KEY[this.paymentStatusLabel(status)];
  }

  paymentStatusClass(status: number): string {
    return STATUS_CLASS[this.paymentStatusLabel(status)];
  }

  methodKey(method: 'cash' | 'card' | 'udhar'): string {
    return `sales.paymentMethods.${method}`;
  }

  get filteredProducts(): Product[] {
    if (!this.search.trim()) return this.products();
    const q = this.search.toLowerCase();
    return this.products().filter(
      (p) =>
        p.productName.toLowerCase().includes(q) ||
        (p.productNameUr ?? '').includes(this.search) ||
        (p.categoryName ?? '').toLowerCase().includes(q) ||
        (p.categoryNameUr ?? '').includes(this.search),
    );
  }

  get productCategories(): string[] {
    return [
      ...new Set(
        this.products()
          .map((p) => p.categoryName)
          .filter((c): c is string => !!c),
      ),
    ];
  }

  get categoryUrMap(): Record<string, string | null> {
    const map: Record<string, string | null> = {};
    for (const p of this.products()) {
      if (p.categoryName && !(p.categoryName in map)) {
        map[p.categoryName] = p.categoryNameUr ?? null;
      }
    }
    return map;
  }

  get pickerProducts(): Product[] {
    return this.products().filter(
      (p) =>
        (this.selectedCategory ? p.categoryName === this.selectedCategory : true) &&
        (this.search ? p.productName.toLowerCase().includes(this.search.toLowerCase()) : true),
    );
  }

  selectProduct(product: Product) {
    if (product.stock === 0) return;
    const existing = this.cart.find((i) => i.productId === product.id);
    if (existing) {
      existing.quantity += this.quantity;
    } else {
      this.cart.push({
        productId: product.id,
        name: product.productName,
        nameUr: product.productNameUr ?? null,
        price: product.price,
        quantity: this.quantity,
      });
    }
    this.search = '';
    this.quantity = 1;
    this.showProductPicker = false;
  }

  addToCart() {
    const match = this.products().find((p) =>
      p.productName.toLowerCase().includes(this.search.toLowerCase()),
    );
    if (match) {
      this.selectProduct(match);
    } else {
      this.showProductPicker = true;
    }
  }

  removeFromCart(productId: string) {
    this.cart = this.cart.filter((i) => i.productId !== productId);
  }

  updateQty(item: CartItem, delta: number) {
    item.quantity += delta;
    if (item.quantity <= 0) this.removeFromCart(item.productId);
  }

  increase() {
    this.quantity++;
  }
  decrease() {
    if (this.quantity > 1) this.quantity--;
  }

  clearCart() {
    this.cart = [];
    this.discount = 0;
    this.selectedCustomerId = '';
    this.paymentMethod = 'cash';
  }

  get totalItems() {
    return this.cart.reduce((s, i) => s + i.quantity, 0);
  }
  get subtotal() {
    return this.cart.reduce((s, i) => s + i.quantity * i.price, 0);
  }
  get totalAmount() {
    return Math.max(0, this.subtotal - this.discount);
  }

  get selectedCustomer(): CustomerOption | null {
    return (
      this.customers().find((c) => c.id === this.selectedCustomerId) ??
      this.customerSearchResults()?.find((c) => c.id === this.selectedCustomerId) ??
      null
    );
  }

  openCheckout() {
    if (this.cart.length === 0) return;
    this.showCheckoutModal = true;
  }

  completeSale() {
    if (this.isSubmitting()) return;

    const status = this.paymentMethod === 'udhar' ? PAYMENT_STATUS.Udhar : PAYMENT_STATUS.Paid;

    const items: SaleItemRequest[] = this.cart.map((i) => ({
      productId: i.productId,
      quantity: i.quantity,
    }));

    const request: SaleAddRequest = {
      customerId: this.selectedCustomerId || null,
      paymentStatus: status,
      note: this.discount > 0 ? `Discount applied: Rs ${this.discount}` : '',
      items,
    };

    if (!this.isOnline()) {
      this.completeSaleOffline(request);
      return;
    }

    this.isSubmitting.set(true);
    this.saleService
      .add(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          this.showCheckoutModal = false;
          this.clearCart();
          this.refreshSalesData();
          this.router.navigate(['/shop-owner-dashboard/invoice', res.data.id]);
        },
        error: (err) => {
          console.error('Failed to complete sale', err);
          this.isSubmitting.set(false);
        },
      });
  }

  private completeSaleOffline(request: SaleAddRequest): void {
    this.isSubmitting.set(true);
    void this.salesRepository.createSale(this.businessId(), request, this.totalAmount).then(() => {
      this.isSubmitting.set(false);
      this.showCheckoutModal = false;
      this.offlineNotice.set(this.t('sales.offline.saleQueued'));
      this.clearCart();
    });
  }

  cancelPendingSale(id: string): void {
    this.cancellingSaleId.set(id);
    void this.salesRepository.cancelPending(id).then((cancelled) => {
      this.cancellingSaleId.set(null);
      if (!cancelled) {
        this.offlineNotice.set(this.t('sales.offline.alreadySyncing'));
      }
    });
  }

  printInvoice() {
    window.print();
  }

  get filteredSales() {
    return this.salesHistory().filter((s) => {
      const statusMatch = this.selectedStatus
        ? this.paymentStatusLabel(s.paymentStatus) === this.selectedStatus
        : true;
      const dateMatch = this.filterDate
        ? new Date(s.date).toDateString() === new Date(this.filterDate).toDateString()
        : true;
      return statusMatch && dateMatch;
    });
  }

  confirmDelete(id: string) {
    this.deleteConfirmId.set(id);
  }

  cancelDelete() {
    this.deleteConfirmId.set(null);
  }

  deleteSale(id: string) {
    if (!this.isOnline()) {
      this.offlineNotice.set(this.t('sales.offline.deleteRequiresConnection'));
      this.deleteConfirmId.set(null);
      return;
    }

    if (this.isDeleting()) return;
    this.isDeleting.set(true);

    this.saleService
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.selectedIds.delete(id);
          this.deleteConfirmId.set(null);
          this.isDeleting.set(false);
          this.refreshSalesData();
        },
        error: (err) => {
          console.error('Failed to delete sale', err);
          this.isDeleting.set(false);
        },
      });
  }

  get allSelected(): boolean {
    return (
      this.filteredSales.length > 0 && this.filteredSales.every((s) => this.selectedIds.has(s.id))
    );
  }

  toggleSelectAll(checked: boolean) {
    if (checked) this.filteredSales.forEach((s) => this.selectedIds.add(s.id));
    else this.filteredSales.forEach((s) => this.selectedIds.delete(s.id));
    this.selectedIds = new Set(this.selectedIds);
  }

  toggleSelect(id: string) {
    this.selectedIds.has(id) ? this.selectedIds.delete(id) : this.selectedIds.add(id);
    this.selectedIds = new Set(this.selectedIds);
  }

  requestBulkDelete() {
    if (!this.selectedIds.size) return;
    if (!this.isOnline()) {
      this.offlineNotice.set(this.t('sales.offline.deleteRequiresConnection'));
      return;
    }
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
        this.saleService.delete(id).pipe(
          catchError((err) => {
            console.error(`Failed to delete sale ${id}`, err);
            return of(null);
          }),
        ),
      ),
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.selectedIds = new Set();
        this.bulkDeleteConfirmOpen.set(false);
        this.isDeleting.set(false);
        this.refreshSalesData();
      });
  }

  get isRecording(): boolean {
    return this.recordingState() === 'recording';
  }

  get isProcessingVoice(): boolean {
    return this.recordingState() === 'processing';
  }

  async toggleRecording(): Promise<void> {
    if (!navigator.onLine) {
      this.voiceError.set(this.t('sales.voice.errors.unsupported'));
      return;
    }

    if (this.isRecording) {
      this.stopRecording();
      return;
    }
    if (this.isProcessingVoice) return;

    this.voiceError.set(null);

    if (!navigator.mediaDevices?.getUserMedia) {
      this.voiceError.set(this.t('sales.voice.errors.unsupported'));
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
      this.voiceError.set(this.t('sales.voice.errors.permissionDenied'));
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
          this.voiceError.set(res?.message || this.t('sales.voice.errors.processFailed'));
          return;
        }

        const voiceRes = res.data;
        if (!voiceRes.success) {
          this.voiceError.set(voiceRes.errorMessage || this.t('sales.voice.errors.notUnderstood'));
          return;
        }

        if (
          voiceRes.intent !== VoiceIntent.CreateSale &&
          voiceRes.intent !== VoiceIntent.AddUdhar
        ) {
          this.voiceError.set(this.t('sales.voice.errors.notASale'));
          return;
        }

        this.voiceSuccessMessage.set(voiceRes.message ?? this.t('sales.voice.saleRecorded'));
        this.refreshSalesData();
      });
  }
}
