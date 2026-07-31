import { NgClass } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
} from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';

export interface CustomerFormModel {
  id?: string;
  name: string;
  phoneNumber: string;
  address: string;
  totalPurchases: number;
  udharAmount: number;
  lastVisit: string;
}

@Component({
  selector: 'app-customer-modal',
  imports: [FormsModule, NgClass, TranslocoDirective],
  templateUrl: './customer-modal.html',
  styleUrl: './customer-modal.css',
})
export class CustomerModal implements OnInit, OnChanges {
  @Input() customer: CustomerFormModel | null = null;
  @Input() isEditMode = false;
  @Input() isSaving = false;

  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<CustomerFormModel>();

  form: CustomerFormModel = this.emptyForm();

  private emptyForm(): CustomerFormModel {
    return {
      name: '',
      phoneNumber: '',
      address: '',
      totalPurchases: 0,
      udharAmount: 0,
      lastVisit: new Date().toISOString(),
    };
  }

  ngOnInit() {
    if (this.customer) {
      this.form = { ...this.customer };
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    console.log('ngOnChanges fired, changes:', Object.keys(changes));
    if (changes['customer']) {
      console.log('customer input changed — resetting form. New value:', this.customer);
      this.form = this.customer ? { ...this.customer } : this.emptyForm();
    }
  }

  onClose() {
    if (this.isSaving) return;
    this.close.emit();
  }

  onSubmit(formRef: NgForm) {
    console.log('onSubmit — form values at submit time:', { ...this.form });
    if (formRef.invalid || this.isSaving) return;
    this.save.emit(this.form);
  }
}
