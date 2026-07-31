import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth-service';
import { LanguageService } from '../../../services/language-service';
import { TranslocoDirective } from '@jsverse/transloco';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslocoDirective],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword {
  private authService = inject(AuthService);
  private languageService = inject(LanguageService);
  private router = inject(Router);

  lang = this.languageService.currentLang;
  readonly dir = computed(() => (this.lang() === 'ur' ? 'rtl' : 'ltr'));

  step = signal(1);
  loading = signal(false);
  errorKey = signal<string | null>(null);

  identifier = '';
  otpDigits = ['', '', '', '', '', ''];
  newPassword = '';
  confirmPassword = '';
  showPassword = false;
  showConfirm = false;
  resendTimer = signal(0);

  private resendInterval: ReturnType<typeof setInterval> | undefined;

  sendOtp(): void {
    this.errorKey.set(null);
    if (!this.identifier.trim()) {
      this.errorKey.set('forgotPassword.errors.missingIdentifier');
      return;
    }

    this.loading.set(true);
    this.authService.forgotPassword({ email: this.identifier }).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (!res.result) {
          this.errorKey.set('forgotPassword.errors.generic');
          return;
        }
        this.step.set(2);
        this.startResendTimer();
      },
      error: (err) => {
        this.loading.set(false);
        this.errorKey.set(
          err.status === 404 ? 'forgotPassword.errors.notFound' : 'forgotPassword.errors.generic',
        );
      },
    });
  }

  onOtpInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    const val = input.value.replace(/\D/g, '').slice(0, 1);
    this.otpDigits[index] = val;
    if (val && index < 5) {
      const next = document.getElementById(`otp-${index + 1}`) as HTMLInputElement | null;
      next?.focus();
    }
  }

  onOtpKeydown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Backspace' && !this.otpDigits[index] && index > 0) {
      const prev = document.getElementById(`otp-${index - 1}`) as HTMLInputElement | null;
      prev?.focus();
    }
  }

  verifyOtp(): void {
    this.errorKey.set(null);
    const otp = this.otpDigits.join('');
    if (otp.length < 6) {
      this.errorKey.set('forgotPassword.errors.incompleteCode');
      return;
    }
    clearInterval(this.resendInterval);
    this.step.set(3);
  }

  startResendTimer(): void {
    this.resendTimer.set(30);
    this.resendInterval = setInterval(() => {
      this.resendTimer.update((v) => v - 1);
      if (this.resendTimer() <= 0) clearInterval(this.resendInterval);
    }, 1000);
  }

  resendOtp(): void {
    this.errorKey.set(null);
    this.loading.set(true);
    this.authService.forgotPassword({ email: this.identifier }).subscribe({
      next: () => {
        this.loading.set(false);
        this.otpDigits = ['', '', '', '', '', ''];
        this.startResendTimer();
      },
      error: () => {
        this.loading.set(false);
        this.errorKey.set('forgotPassword.errors.generic');
      },
    });
  }

  get passwordStrength(): number {
    const p = this.newPassword;
    if (!p) return 0;
    let score = 0;
    if (p.length >= 8) score++;
    if (/[A-Z]/.test(p)) score++;
    if (/[0-9]/.test(p)) score++;
    if (/[^A-Za-z0-9]/.test(p)) score++;
    return score;
  }

  readonly strengthLabelKeys = [
    '',
    'forgotPassword.strength.weak',
    'forgotPassword.strength.fair',
    'forgotPassword.strength.good',
    'forgotPassword.strength.strong',
  ];

  get passwordStrengthLabelKey(): string {
    return this.strengthLabelKeys[this.passwordStrength];
  }

  resetPassword(): void {
    this.errorKey.set(null);
    if (this.newPassword.length < 8) {
      this.errorKey.set('forgotPassword.errors.tooShort');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.errorKey.set('forgotPassword.errors.mismatch');
      return;
    }

    this.loading.set(true);
    this.authService
      .resetPassword({
        email: this.identifier,
        token: this.otpDigits.join(''),
        newPassword: this.newPassword,
      })
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (!res.result) {
            this.errorKey.set('forgotPassword.errors.invalidToken');
            this.step.set(2);
            this.otpDigits = ['', '', '', '', '', ''];
            return;
          }
          this.step.set(4);
        },
        error: () => {
          this.loading.set(false);
          this.errorKey.set('forgotPassword.errors.invalidToken');
          this.step.set(2);
          this.otpDigits = ['', '', '', '', '', ''];
        },
      });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}