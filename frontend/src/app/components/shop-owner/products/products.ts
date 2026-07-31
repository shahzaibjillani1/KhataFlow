import { DecimalPipe, NgClass } from '@angular/common';
import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, catchError, of } from 'rxjs';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { ProductModal } from '../../../shared/components/product-modal/product-modal';
import { ProductService } from '../../../services/product-service';
import { CategoryService } from '../../../services/category-service';
import {
  Product,
  ProductAddRequest,
  ProductUpdateRequest,
} from '../../../core/models/product-models';
import { CategoryAddRequest } from '../../../core/models/category-models';
import { PaginatedResponse } from '../../../core/models/paginated-response-model';
import { LanguageService } from '../../../services/language-service';
import { LocalizedTextPipe } from '../../../shared/pipes/localized-text-pipe';
import { AuthService } from '../../../services/auth-service';
import { SyncService } from '../../../core/offline/sync-service';
import { LocalProduct } from '../../../core/offline/khataflow-db';
import {
  CategoryNotResolvedError,
  ProductRepository,
} from '../../../services/product-respository-service';
import { BusinessSubscriptionService } from '../../../services/business-subscription-service';

type SortField = 'productName' | 'price' | 'stock' | 'categoryName';
type SortDir = 'asc' | 'desc';
type StockStatus = 'In Stock' | 'Low Stock' | 'Out of Stock';

interface ProductFormModel {
  id: string;
  productName: string;
  categoryId: string;
  price: number;
  stock: number;
}

interface CategoryFormModel {
  categoryName: string;
}

type DisplayProduct = Product & { pendingSync?: boolean };

const LOW_STOCK_THRESHOLD = 5;
const PAGE_SIZE = 10;

const FETCH_ALL_PAGE_SIZE = 100;

@Component({
  selector: 'app-products',
  imports: [NgClass, FormsModule, DecimalPipe, ProductModal, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './products.html',
  styleUrl: './products.css',
})
export class Products implements OnInit {
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private languageService = inject(LanguageService);
  private transloco = inject(TranslocoService);
  private authService = inject(AuthService);
  private subscriptionService = inject(BusinessSubscriptionService);

  private readonly productRepository = inject(ProductRepository);
  private readonly syncService = inject(SyncService);
  private readonly businessId = signal(this.authService.getCurrentBusinessId() ?? '');
  readonly isOnline = signal(navigator.onLine);
  readonly offlineError = signal<string | null>(null);

  lang = this.languageService.currentLang;
  readonly role = this.authService.getRole();
  readonly isStaff = this.role === 'Staff';
  readonly canSeeOwnerOrManager = this.role === 'Owner' || this.role === 'Manager';

  search = '';
  selectedCategory = '';
  selectedStatus = '';
  sortField: SortField = 'productName';
  sortDir: SortDir = 'asc';
  currentPage = 1;
  readonly pageSize = PAGE_SIZE;

  isLoading = signal(true);
  loadError = signal<string | null>(null);
  isSaving = signal(false);
  isDeleting = signal(false);

  catalogTruncated = signal(false);

  readonly isPremium = this.subscriptionService.isPremium;

  isModalOpen = false;
  isEditMode = false;
  deleteConfirmId: string | null = null;
  bulkDeleteConfirmOpen = signal(false);

  selectedIds = new Set<string>();

  form: ProductFormModel = this.emptyForm();

  private emptyForm(): ProductFormModel {
    return { id: '', productName: '', categoryId: '', price: 0, stock: 0 };
  }

  readonly isCategoryModalOpen = signal(false);
  readonly isSavingCategory = signal(false);
  readonly categorySaveError = signal<string | null>(null);
  categoryForm: CategoryFormModel = this.emptyCategoryForm();

  private emptyCategoryForm(): CategoryFormModel {
    return { categoryName: '' };
  }

  private readonly serverProducts = signal<Product[]>([]);
  categories = this.categoryService.categories;

  readonly products = computed<DisplayProduct[]>(() => {
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
      pendingSync: !!p.isDirty,
    }));
  });

  private t(key: string, params?: Record<string, unknown>): string {
    return this.transloco.translate(key, params);
  }

  constructor() {
    window.addEventListener('online', () => this.isOnline.set(true));
    window.addEventListener('offline', () => this.isOnline.set(false));

    effect(() => {
      if (this.syncService.state() === 'idle' && this.isOnline()) {
        this.loadProducts();
      }
    });
  }

  ngOnInit() {
    void this.productRepository.loadForBusiness(this.businessId());
    this.retry();
  }

  retry() {
    this.loadCategories();
    this.loadProducts();
  }

  private loadCategories() {
    if (!navigator.onLine) return;
    this.categoryService.fetchAll().subscribe({
      error: (err) => {
        console.error('Failed to load categories', err);
      },
    });
  }

  private loadProducts() {
    if (!navigator.onLine) {
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.loadError.set(null);
    this.productService.getPaged(1, FETCH_ALL_PAGE_SIZE).subscribe({
      next: (res: { data: PaginatedResponse<Product> }) => {
        this.serverProducts.set(res.data.items);
        this.catalogTruncated.set(res.data.totalCount > FETCH_ALL_PAGE_SIZE);
        this.isLoading.set(false);
        void this.productRepository.refreshFromServer(this.businessId());
      },
      error: (err) => {
        console.error('Failed to load products', err);
        this.loadError.set(this.t('products.errors.loadFailed'));
        this.isLoading.set(false);
      },
    });
  }

  status(product: Product): StockStatus {
    if (product.stock === 0) return 'Out of Stock';
    if (product.stock <= LOW_STOCK_THRESHOLD) return 'Low Stock';
    return 'In Stock';
  }

  get filteredProducts(): DisplayProduct[] {
    const q = this.search.toLowerCase();
    let list = this.products().filter((p) => {
      const matchSearch =
        !q ||
        p.productName.toLowerCase().includes(q) ||
        (p.productNameUr ?? '').includes(this.search);
      const matchCategory =
        !this.selectedCategory || this.categoryIdFor(p) === this.selectedCategory;
      const matchStatus = !this.selectedStatus || this.status(p) === this.selectedStatus;
      return matchSearch && matchCategory && matchStatus;
    });

    list = [...list].sort((a, b) => {
      let va: any = a[this.sortField];
      let vb: any = b[this.sortField];
      if (typeof va === 'string') va = va.toLowerCase();
      if (typeof vb === 'string') vb = vb.toLowerCase();
      if (va == null) va = '';
      if (vb == null) vb = '';
      if (va < vb) return this.sortDir === 'asc' ? -1 : 1;
      if (va > vb) return this.sortDir === 'asc' ? 1 : -1;
      return 0;
    });

    return list;
  }

  get tableColspan(): number {
    return this.canSeeOwnerOrManager ? 7 : 5;
  }

  private categoryIdFor(product: Product): string | undefined {
    return this.categories().find((c) => c.categoryName === product.categoryName)?.id;
  }

  get pagedProducts(): DisplayProduct[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredProducts.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredProducts.length / this.pageSize));
  }

  get paginationItems(): (number | null)[] {
    const total = this.totalPages;
    const current = this.currentPage;
    const delta = 1;
    const items: (number | null)[] = [1];

    const left = Math.max(2, current - delta);
    const right = Math.min(total - 1, current + delta);

    if (left > 2) items.push(null);
    for (let i = left; i <= right; i++) items.push(i);
    if (right < total - 1) items.push(null);
    if (total > 1) items.push(total);

    return items;
  }

  get inStockCount() {
    return this.products().filter((p) => this.status(p) === 'In Stock').length;
  }
  get lowStockCount() {
    return this.products().filter((p) => this.status(p) === 'Low Stock').length;
  }
  get outOfStockCount() {
    return this.products().filter((p) => this.status(p) === 'Out of Stock').length;
  }

  setSort(field: SortField) {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir = 'asc';
    }
    this.currentPage = 1;
  }

  sortIcon(field: SortField): string {
    if (this.sortField !== field) return 'fa-sort';
    return this.sortDir === 'asc' ? 'fa-sort-up' : 'fa-sort-down';
  }

  goToPage(p: number) {
    if (p >= 1 && p <= this.totalPages) this.currentPage = p;
  }

  openAddModal() {
    if (!this.isOnline()) {
      this.offlineError.set(this.t('products.offline.addRequiresConnection'));
      return;
    }
    this.offlineError.set(null);
    this.form = this.emptyForm();
    this.isEditMode = false;
    this.isModalOpen = true;
  }

  editProduct(product: DisplayProduct) {
    this.offlineError.set(null);
    this.form = {
      id: product.id,
      productName: product.productName,
      categoryId: this.categoryIdFor(product) ?? '',
      price: product.price,
      stock: product.stock,
    };
    this.isEditMode = true;
    this.isModalOpen = true;
  }

  saveProduct(saved: ProductFormModel) {
    if (this.isSaving()) return;

    if (!this.isOnline()) {
      this.saveProductOffline(saved);
      return;
    }

    this.isSaving.set(true);

    if (this.isEditMode) {
      const request: ProductUpdateRequest = {
        id: saved.id,
        productName: saved.productName,
        categoryId: saved.categoryId,
        price: saved.price,
        stock: saved.stock,
        inventoryStatus: 0, 
      };
      this.productService.update(saved.id, request).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.isModalOpen = false;
          this.loadProducts();
        },
        error: (err) => {
          console.error('Failed to update product', err);
          this.isSaving.set(false);
        },
      });
    } else {
      const request: ProductAddRequest = {
        productName: saved.productName,
        categoryId: saved.categoryId,
        price: saved.price,
        stock: saved.stock,
        inventoryStatus: 0,
      };
      this.productService.add(request).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.isModalOpen = false;
          this.loadProducts();
        },
        error: (err) => {
          console.error('Failed to add product', err);
          this.isSaving.set(false);
        },
      });
    }
  }

  private saveProductOffline(saved: ProductFormModel): void {
    const original = this.products().find((p) => p.id === saved.id);
    const originalCategoryId = original ? this.categoryIdFor(original) : undefined;

    const nameChanged = original && saved.productName !== original.productName;
    const categoryChanged = original && saved.categoryId !== (originalCategoryId ?? '');

    if (!this.isEditMode || nameChanged || categoryChanged) {
      this.offlineError.set(this.t('products.offline.onlyPriceStockOffline'));
      return;
    }

    this.isSaving.set(true);
    this.productRepository
      .quickEdit(this.businessId(), { productId: saved.id, price: saved.price, stock: saved.stock })
      .then(() => {
        this.isSaving.set(false);
        this.isModalOpen = false;
      })
      .catch((err) => {
        this.isSaving.set(false);
        if (err instanceof CategoryNotResolvedError) {
          this.offlineError.set(this.t('products.offline.categoryNotResolved'));
        } else {
          console.error('Offline quick edit failed', err);
          this.offlineError.set(this.t('products.errors.saveFailed'));
        }
      });
  }

  confirmDelete(id: string) {
    this.deleteConfirmId = id;
  }

  cancelDelete() {
    this.deleteConfirmId = null;
  }

  deleteProduct(id: string) {
    if (!this.isOnline()) {
      this.offlineError.set(this.t('products.offline.deleteRequiresConnection'));
      this.deleteConfirmId = null;
      return;
    }

    if (this.isDeleting()) return;
    this.isDeleting.set(true);

    this.productService.delete(id).subscribe({
      next: () => {
        this.selectedIds.delete(id);
        this.deleteConfirmId = null;
        this.isDeleting.set(false);
        this.loadProducts();
        this.currentPage = Math.min(this.currentPage, this.totalPages);
      },
      error: (err) => {
        console.error('Failed to delete product', err);
        this.isDeleting.set(false);
      },
    });
  }

  get allPageSelected(): boolean {
    return (
      this.pagedProducts.length > 0 && this.pagedProducts.every((p) => this.selectedIds.has(p.id))
    );
  }

  toggleSelectAll(checked: boolean) {
    if (checked) this.pagedProducts.forEach((p) => this.selectedIds.add(p.id));
    else this.pagedProducts.forEach((p) => this.selectedIds.delete(p.id));
    this.selectedIds = new Set(this.selectedIds);
  }

  toggleSelect(id: string) {
    this.selectedIds.has(id) ? this.selectedIds.delete(id) : this.selectedIds.add(id);
    this.selectedIds = new Set(this.selectedIds);
  }

  requestBulkDelete() {
    if (!this.selectedIds.size) return;
    if (!this.isOnline()) {
      this.offlineError.set(this.t('products.offline.deleteRequiresConnection'));
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
        this.productService.delete(id).pipe(
          catchError((err) => {
            console.error(`Failed to delete product ${id}`, err);
            return of(null);
          }),
        ),
      ),
    ).subscribe(() => {
      this.selectedIds = new Set();
      this.bulkDeleteConfirmOpen.set(false);
      this.isDeleting.set(false);
      this.loadProducts();
    });
  }

  exportCsv() {
    const activeLang = this.lang();
    const localize = (en: string, ur: string | null | undefined) =>
      activeLang === 'ur' && ur?.trim() ? ur : en;

    const header = ['ID', 'Name', 'Category', 'Price (Rs)', 'Stock', 'Status'];
    const rows = this.filteredProducts.map((p) => [
      p.id,
      `"${localize(p.productName, p.productNameUr)}"`,
      localize(p.categoryName ?? '', p.categoryNameUr),
      p.price,
      p.stock,
      this.status(p),
    ]);

    const csv = [header, ...rows].map((r) => r.join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = `products_${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  statusKey(status: StockStatus): string {
    return {
      'In Stock': 'products.status.inStock',
      'Low Stock': 'products.status.lowStock',
      'Out of Stock': 'products.status.outOfStock',
    }[status];
  }

  openAddCategoryModal(): void {
    if (!this.isOnline()) {
      this.offlineError.set(this.t('products.offline.addRequiresConnection'));
      return;
    }
    this.offlineError.set(null);
    this.categorySaveError.set(null);
    this.categoryForm = this.emptyCategoryForm();
    this.isCategoryModalOpen.set(true);
  }

  closeCategoryModal(): void {
    if (this.isSavingCategory()) return; // don't let a stray click cancel an in-flight save
    this.isCategoryModalOpen.set(false);
  }

  saveCategory(): void {
    if (this.isSavingCategory()) return;

    const name = this.categoryForm.categoryName.trim();
    if (!name) {
      this.categorySaveError.set(this.t('products.category.nameRequired'));
      return;
    }

    const duplicate = this.categories().some(
      (c) => c.categoryName.trim().toLowerCase() === name.toLowerCase(),
    );
    if (duplicate) {
      this.categorySaveError.set(this.t('products.category.duplicate'));
      return;
    }

    this.categorySaveError.set(null);
    this.isSavingCategory.set(true);

    const request: CategoryAddRequest = { categoryName: name };
    this.categoryService.add(request).subscribe({
      next: (res) => {
        this.isSavingCategory.set(false);
        this.isCategoryModalOpen.set(false);
        this.selectedCategory = res.data.id;
        this.currentPage = 1;
      },
      error: (err) => {
        console.error('Failed to add category', err);
        this.isSavingCategory.set(false);
        this.categorySaveError.set(this.t('products.category.saveFailed'));
      },
    });
  }
}
