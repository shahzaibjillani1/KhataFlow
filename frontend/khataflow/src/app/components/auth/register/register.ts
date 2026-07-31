import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../services/auth-service';
import { LanguageService } from '../../../services/language-service';
import { TranslocoDirective } from '@jsverse/transloco';
import { RegisterRequest } from '../../../core/models/auth-models';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, RouterOutlet, TranslocoDirective],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private authService = inject(AuthService);
  private router = inject(Router);
  private languageService = inject(LanguageService);

  lang = this.languageService.currentLang;
  readonly dir = computed(() => (this.lang() === 'ur' ? 'rtl' : 'ltr'));

  businessName = '';
  fullName = '';
  email = '';
  phoneNumber = '';
  password = '';

  isLoading = signal(false);
  errorMessageKey = signal<string | null>(null);
  showPassword = signal(false);

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  register(): void {
    if (
      !this.businessName ||
      !this.fullName ||
      !this.email ||
      !this.phoneNumber ||
      !this.password
    ) {
      this.errorMessageKey.set('register.errors.missingFields');
      return;
    }

    this.isLoading.set(true);
    this.errorMessageKey.set(null);

    const request: RegisterRequest = {
      businessName: this.businessName,
      fullName: this.fullName,
      email: this.email,
      phoneNumber: this.phoneNumber,
      password: this.password,
    };

    this.authService.register(request).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (!res.result) {
          this.errorMessageKey.set('register.errors.generic');
          return;
        }
        const role = this.authService.getRole();
        this.router.navigate([this.dashboardRouteForRole(role)]);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessageKey.set(
          err.status === 409 ? 'register.errors.emailExists' : 'register.errors.generic',
        );
      },
    });
  }

  private dashboardRouteForRole(role: string | null): string {
    switch (role) {
      case 'SuperAdmin':
        return '/admin-dashboard';
      case 'Owner':
      case 'Manager':
      case 'Staff':
        return '/shop-owner-dashboard';
      default:
        return '/login';
    }
  }
}
