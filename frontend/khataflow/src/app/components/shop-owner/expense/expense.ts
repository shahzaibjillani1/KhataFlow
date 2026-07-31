import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin, catchError, of } from 'rxjs';
import { ExpenseService } from '../../../services/expense-service';
import { AiService } from '../../../services/ai-service';
import { ExpenseCategory } from '../../../core/enums/expense-category';
import * as XLSX from 'xlsx';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { VoiceIntent } from '../../../core/models/ai-models';
import { LocalizedTextPipe } from '../../../shared/pipes/localized-text-pipe';
import { LanguageService } from '../../../services/language-service';
import { Expense as ExpenseModel, ExpenseAddRequest } from '../../../core/models/expense-models';

import { ExpenseRepository } from '../../../services/expense-repository-service';
import { SyncService } from '../../../core/offline/sync-service';
import { AuthService } from '../../../services/auth-service';
import { BusinessSubscriptionService } from '../../../services/business-subscription-service';

type SortKey = 'title' | 'category' | 'amount' | 'date';
type RecordingState = 'idle' | 'recording' | 'processing';

interface DisplayExpense extends ExpenseModel {
  pendingSync?: boolean;
}

const FETCH_ALL_PAGE_SIZE = 100;
const CLIENT_PAGE_SIZE = 10;

@Component({
  selector: 'app-expense',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './expense.html',
  styleUrl: './expense.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Expense implements OnInit {
  private readonly expenseService = inject(ExpenseService);
  private readonly aiService = inject(AiService);
  private readonly fb = inject(FormBuilder);
  private readonly translocoService = inject(TranslocoService);
  private readonly languageService = inject(LanguageService);
  private subscriptionService = inject(BusinessSubscriptionService);

  readonly isPremium = this.subscriptionService.isPremium;

  private readonly expenseRepository = inject(ExpenseRepository);
  private readonly syncService = inject(SyncService);
  private readonly authService = inject(AuthService);

  private readonly businessId = signal(this.authService.getCurrentBusinessId() ?? '');
  readonly isOnline = signal(navigator.onLine);
  readonly pendingExpenses = computed(() =>
    this.expenseRepository.expenses().filter((e) => e.syncStatus === 'pending'),
  );

  readonly voiceSuccessMessage = signal<string | null>(null);

  readonly lang = this.languageService.currentLang;

  readonly role = this.authService.getRole();
  readonly canSeeOwnerOrManager = this.role === 'Owner' || this.role === 'Manager';

  readonly expenses = this.expenseService.expenses;
  readonly totalExpense = this.expenseService.totalExpense;
  readonly loading = this.expenseService.loading;
  readonly loadError = signal<string | null>(null);

  readonly catalogTruncated = computed(
    () => this.expenseService.totalCount() > FETCH_ALL_PAGE_SIZE,
  );

  readonly isModalOpen = signal(false);
  readonly isSubmitting = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly deleteConfirmId = signal<string | null>(null);
  readonly formError = signal<string | null>(null);

  readonly recordingState = signal<RecordingState>('idle');
  readonly voiceError = signal<string | null>(null);
  readonly voicePrefillNotice = signal<string | null>(null);
  private mediaRecorder: MediaRecorder | null = null;
  private audioChunks: Blob[] = [];

  readonly searchTerm = signal('');
  readonly categoryFilter = signal<ExpenseCategory | 'all'>('all');
  readonly sortKey = signal<SortKey>('date');
  readonly sortAsc = signal(false);
  readonly currentPage = signal(1);
  readonly clientPageSize = CLIENT_PAGE_SIZE;

  private readonly fromDate = this.toDateInput(this.daysAgo(30));
  private readonly toDate = this.toDateInput(new Date());

  private readonly t = (key: string) => this.translocoService.translate(key);

  readonly categories = Object.entries(ExpenseCategory)
    .filter(([key]) => isNaN(Number(key)))
    .map(([label, value]) => ({ label, value: value as ExpenseCategory }));

  selectedIds = new Set<string>();
  readonly bulkDeleteConfirmOpen = signal(false);
  readonly isBulkDeleting = signal(false);

  private readonly mergedExpenses = computed<DisplayExpense[]>(() => {
    const pending: DisplayExpense[] = this.pendingExpenses().map((local) => ({
      id: local.id,
      title: local.request.title,
      titleUr: null,
      amount: local.request.amount,
      category: local.request.category,
      note: local.request.note ?? null,
      noteUr: null,
      date: local.request.date ?? new Date(local.createdAt).toISOString(),
      pendingSync: true,
    }));

    return [...pending, ...this.expenses()];
  });

  readonly filteredExpenses = computed(() => {
    let list = [...this.mergedExpenses()];

    const term = this.searchTerm().trim().toLowerCase();
    if (term) list = list.filter((e) => e.title.toLowerCase().includes(term));

    const cat = this.categoryFilter();
    if (cat !== 'all') list = list.filter((e) => e.category === cat);

    const key = this.sortKey();
    const asc = this.sortAsc();
    list.sort((a, b) => {
      let cmp = 0;
      if (key === 'title') cmp = a.title.localeCompare(b.title);
      if (key === 'category') cmp = a.category - b.category;
      if (key === 'amount') cmp = a.amount - b.amount;
      if (key === 'date') cmp = new Date(a.date).getTime() - new Date(b.date).getTime();
      return asc ? cmp : -cmp;
    });

    return list;
  });

  readonly resultCount = computed(() => this.filteredExpenses().length);

  readonly totalClientPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredExpenses().length / this.clientPageSize)),
  );

  readonly pagedExpenses = computed(() => {
    const start = (this.currentPage() - 1) * this.clientPageSize;
    return this.filteredExpenses().slice(start, start + this.clientPageSize);
  });

  readonly paginationItems = computed<(number | null)[]>(() => {
    const total = this.totalClientPages();
    const current = this.currentPage();
    const delta = 1;
    const items: (number | null)[] = [1];

    const left = Math.max(2, current - delta);
    const right = Math.min(total - 1, current + delta);

    if (left > 2) items.push(null);
    for (let i = left; i <= right; i++) items.push(i);
    if (right < total - 1) items.push(null);
    if (total > 1) items.push(total);

    return items;
  });

  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(100)]],
    amount: [null, [Validators.required, Validators.min(1)]],
    category: [ExpenseCategory.Miscellaneous, Validators.required],
    note: [''],
  });

  constructor() {
    effect(() => {
      if (this.syncService.state() === 'idle') {
        void this.expenseRepository.reconcile(this.businessId());
      }
    });

    window.addEventListener('online', () => this.isOnline.set(true));
    window.addEventListener('offline', () => this.isOnline.set(false));
  }

  ngOnInit(): void {
    void this.expenseRepository.loadForBusiness(this.businessId());
    this.loadData();
  }

  private loadData(): void {
    if (!navigator.onLine) return; 

    this.loadError.set(null);
    forkJoin({
      expenses: this.expenseService.getAll(1, FETCH_ALL_PAGE_SIZE).pipe(
        catchError(() => {
          this.loadError.set(this.t('expense.errors.loadFailed'));
          return of(null);
        }),
      ),
      total: this.expenseService
        .getTotal(this.fromDate, this.toDate)
        .pipe(catchError(() => of(null))),
    }).subscribe();
  }

  retry(): void {
    this.loadData();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalClientPages()) {
      this.currentPage.set(page);
    }
  }

  resetToFirstPage(): void {
    this.currentPage.set(1);
    this.selectedIds = new Set();
  }

  toggleSort(key: SortKey): void {
    if (this.sortKey() === key) {
      this.sortAsc.update((v) => !v);
    } else {
      this.sortKey.set(key);
      this.sortAsc.set(true);
    }
    this.currentPage.set(1);
  }

  sortIcon(key: SortKey): string {
    if (this.sortKey() !== key) return '↕';
    return this.sortAsc() ? '↑' : '↓';
  }


  get allPageSelected(): boolean {
    const page = this.pagedExpenses();
    return page.length > 0 && page.every((e) => this.selectedIds.has(e.id));
  }

  toggleSelectAll(checked: boolean): void {
    if (checked) this.pagedExpenses().forEach((e) => this.selectedIds.add(e.id));
    else this.pagedExpenses().forEach((e) => this.selectedIds.delete(e.id));
    this.selectedIds = new Set(this.selectedIds);
  }

  toggleSelect(id: string): void {
    this.selectedIds.has(id) ? this.selectedIds.delete(id) : this.selectedIds.add(id);
    this.selectedIds = new Set(this.selectedIds);
  }

  clearSelection(): void {
    this.selectedIds = new Set();
  }

  requestBulkDelete(): void {
    if (!this.selectedIds.size) return;
    this.bulkDeleteConfirmOpen.set(true);
  }

  cancelBulkDelete(): void {
    this.bulkDeleteConfirmOpen.set(false);
  }

  bulkDelete(): void {
    if (this.isBulkDeleting() || !this.selectedIds.size) return;
    const ids = Array.from(this.selectedIds);

    this.isBulkDeleting.set(true);

    Promise.all(
      ids.map((id) =>
        this.isPendingId(id)
          ? this.expenseRepository.cancelPending(id).catch(() => false)
          : this.expenseService
              .delete(id)
              .pipe(catchError(() => of(null)))
              .toPromise()
              .then((res) => !!res?.result)
              .catch(() => false),
      ),
    ).then(() => {
      this.selectedIds = new Set();
      this.bulkDeleteConfirmOpen.set(false);
      this.isBulkDeleting.set(false);

      this.expenseService.getTotal(this.fromDate, this.toDate).subscribe();

      if (this.currentPage() > this.totalClientPages()) {
        this.currentPage.set(this.totalClientPages());
      }
    });
  }

  // ---------- Language ----------

  onLanguageChange(lang: string): void {
    this.translocoService.setActiveLang(lang);
    localStorage.setItem('lang', lang);
  }

  categoryKey(category: ExpenseCategory): string {
    const name = ExpenseCategory[category] ?? 'miscellaneous';
    return `expense.categories.${name.charAt(0).toLowerCase()}${name.slice(1)}`;
  }


  get isRecording(): boolean {
    return this.recordingState() === 'recording';
  }

  get isProcessingVoice(): boolean {
    return this.recordingState() === 'processing';
  }

  async toggleRecording(): Promise<void> {
    if (!navigator.onLine) {
      this.voiceError.set(this.t('expense.voice.errors.unsupported'));
      return;
    }

    if (this.isRecording) {
      this.stopRecording();
      return;
    }
    if (this.isProcessingVoice) return;

    this.voiceError.set(null);

    if (!navigator.mediaDevices?.getUserMedia) {
      this.voiceError.set(this.t('expense.voice.errors.unsupported'));
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
      this.voiceError.set(this.t('expense.voice.errors.permissionDenied'));
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
      .pipe(
        catchError(() => {
          this.voiceError.set(this.t('expense.voice.errors.processFailed'));
          return of(null);
        }),
      )
      .subscribe((res) => {
        this.recordingState.set('idle');
        if (!res || !res.data) {
          this.voiceError.set(res?.message || this.t('expense.voice.errors.processFailed'));
          return;
        }

        const voiceRes = res.data;
        if (!voiceRes.success) {
          this.voiceError.set(
            voiceRes.errorMessage || this.t('expense.voice.errors.notUnderstood'),
          );
          return;
        }

        if (voiceRes.intent !== VoiceIntent.CreateExpense) {
          this.voiceError.set(this.t('expense.voice.errors.notAnExpense'));
          return;
        }

        this.voiceSuccessMessage.set(voiceRes.message ?? this.t('expense.voice.expenseRecorded'));
        this.expenseService.getAll(this.currentPage(), FETCH_ALL_PAGE_SIZE).subscribe();
        this.expenseService.getTotal(this.fromDate, this.toDate).subscribe();
      });
  }


  openModal(): void {
    this.form.reset({ category: ExpenseCategory.Miscellaneous });
    this.formError.set(null);
    this.voicePrefillNotice.set(null);
    this.isModalOpen.set(true);
  }

  closeModal(): void {
    if (this.isSubmitting()) return;
    this.isModalOpen.set(false);
    this.voicePrefillNotice.set(null);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.isSubmitting.set(true);
    this.formError.set(null);

    const { title, amount, category, note } = this.form.value;
    const request: ExpenseAddRequest = {
      title,
      amount,
      category,
      note: note || null,
      date: new Date().toISOString(),
    };

    if (!navigator.onLine) {
      void this.expenseRepository.createExpense(this.businessId(), request).then(() => {
        this.isSubmitting.set(false);
        this.closeModal();
      });
      return;
    }

    this.expenseService
      .add(request)
      .pipe(
        catchError(() => {
          this.formError.set(this.t('expense.errors.addFailed'));
          this.isSubmitting.set(false);
          return of(null);
        }),
      )
      .subscribe((res) => {
        this.isSubmitting.set(false);
        if (res?.result) {
          this.closeModal();
          this.expenseService.getTotal(this.fromDate, this.toDate).subscribe();
        } else if (res && !res.result) {
          this.formError.set(res.message || this.t('expense.errors.addFailed'));
        }
      });
  }

  confirmDelete(id: string): void {
    this.deleteConfirmId.set(id);
  }

  cancelDelete(): void {
    this.deleteConfirmId.set(null);
  }

  isPendingId(id: string): boolean {
    return this.pendingExpenses().some((e) => e.id === id);
  }

  deleteExpense(id: string): void {
    if (this.isPendingId(id)) {
      this.deletingId.set(id);
      void this.expenseRepository.cancelPending(id).then((cancelled) => {
        this.deletingId.set(null);
        this.deleteConfirmId.set(null);
        if (!cancelled) {
          this.formError.set(this.t('expense.errors.alreadySyncing'));
        }
      });
      return;
    }

    this.deletingId.set(id);
    this.expenseService
      .delete(id)
      .pipe(catchError(() => of(null)))
      .subscribe((res) => {
        this.deletingId.set(null);
        this.deleteConfirmId.set(null);
        if (res?.result) {
          this.expenseService.getTotal(this.fromDate, this.toDate).subscribe();
          if (this.currentPage() > this.totalClientPages()) {
            this.currentPage.set(this.totalClientPages());
          }
        }
      });
  }

  categoryLabel(category: ExpenseCategory): string {
    return ExpenseCategory[category] ?? 'Unknown';
  }

  exportToExcel(): void {
    const rows = this.filteredExpenses().map((e) => ({
      Title: e.title,
      Category: this.categoryLabel(e.category),
      Note: e.note ?? '',
      Date: new Date(e.date).toLocaleDateString(),
      'Amount (Rs)': e.amount,
    }));

    if (!rows.length) return;

    const worksheet = XLSX.utils.json_to_sheet(rows);
    worksheet['!cols'] = [{ wch: 24 }, { wch: 16 }, { wch: 30 }, { wch: 14 }, { wch: 14 }];

    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Expenses');

    const fileName = `expenses_${this.toDateInput(new Date())}.xlsx`;
    XLSX.writeFile(workbook, fileName);
  }

  private daysAgo(days: number): Date {
    const d = new Date();
    d.setDate(d.getDate() - days);
    return d;
  }

  private toDateInput(date: Date): string {
    return date.toISOString().split('T')[0];
  }
}
