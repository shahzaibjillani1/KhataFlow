import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { StaffService } from '../../../services/staff-service';
import { UserRole } from '../../../core/enums/user-role';

@Component({
  selector: 'app-staff-invite',
  imports: [CommonModule, ReactiveFormsModule, TranslocoDirective],
  templateUrl: './staff-invite.html',
  styleUrl: './staff-invite.css',
})
export class StaffInvite {
  private fb = inject(FormBuilder);
  private staffService = inject(StaffService);
  private translocoService = inject(TranslocoService);

  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  whatsAppUrl = signal<string | null>(null);
  invitedName = signal<string | null>(null);

  assignableRoles: UserRole[] = ['Manager', 'Staff'];

  form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?\d{10,15}$/)]],
    role: ['Staff' as UserRole, Validators.required],
  });

  get fullName() {
    return this.form.controls.fullName;
  }
  get email() {
    return this.form.controls.email;
  }
  get phoneNumber() {
    return this.form.controls.phoneNumber;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.whatsAppUrl.set(null);

    const formValue = this.form.getRawValue();

    this.staffService.inviteStaff(formValue).subscribe({
      next: (res) => {
        this.successMessage.set(this.translocoService.translate('staffInvite.successMessage'));
        this.whatsAppUrl.set(res.data?.whatsAppShareUrl ?? null);
        this.invitedName.set(res.data?.user?.fullName ?? formValue.fullName);
        this.form.reset({ role: 'Staff' });
        this.isSubmitting.set(false);
      },
      error: (err) => {
        this.errorMessage.set(
          err?.error?.message ??
            this.translocoService.translate('staffInvite.errorMessageFallback'),
        );
        this.isSubmitting.set(false);
      },
    });
  }

  openWhatsApp(): void {
    const url = this.whatsAppUrl();
    console.log('whatsAppUrl:', url);
    if (url) window.open(url, '_blank', 'noopener');
  }
}
