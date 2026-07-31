import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { CategoryService } from '../../../services/category-service';
import { LanguageService } from '../../../services/language-service';
import { LocalizedTextPipe } from '../../pipes/localized-text-pipe';

interface ProductFormModel {
  id: string;
  productName: string;
  categoryId: string;
  price: number;
  stock: number;
}

@Component({
  selector: 'app-product-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './product-modal.html',
})
export class ProductModal implements OnInit, OnChanges {
  @Input() isEditMode = false;
  @Input() product!: ProductFormModel;
  @Input() isSaving = false; 

  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<ProductFormModel>();

  private categoryService = inject(CategoryService);
  private languageService = inject(LanguageService);

  lang = this.languageService.currentLang;

  categories = this.categoryService.categories;

  form: ProductFormModel = this.emptyForm();
  submitted = false;

  private emptyForm(): ProductFormModel {
    return { id: '', productName: '', categoryId: '', price: 0, stock: 0 };
  }

  ngOnInit() {
    if (this.categories().length === 0) {
      this.categoryService.fetchAll().subscribe({
        error: (err) => console.error('Failed to load categories', err),
      });
    }
    this.syncForm();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['product']) {
      this.syncForm();
    }
  }

  private syncForm() {
    this.form = this.product ? { ...this.product } : this.emptyForm();
    this.submitted = false;
  }

  get isNameInvalid(): boolean {
    return this.submitted && !this.form.productName.trim();
  }

  get isCategoryInvalid(): boolean {
    return this.submitted && !this.form.categoryId;
  }

  get isPriceInvalid(): boolean {
    return this.submitted && this.form.price <= 0;
  }

  get isStockInvalid(): boolean {
    return this.submitted && this.form.stock < 0;
  }

  onSave() {
    if (this.isSaving) return; 

    this.submitted = true;
    if (
      !this.form.productName.trim() ||
      !this.form.categoryId ||
      this.form.price <= 0 ||
      this.form.stock < 0
    ) {
      return;
    }
    this.save.emit(this.form);
  }

  onClose() {
    if (this.isSaving) return; 
    this.close.emit();
  }
}
