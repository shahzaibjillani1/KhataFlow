import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../services/auth-service';
import { LanguageService } from '../../../services/language-service';
import { TranslocoDirective } from '@jsverse/transloco';
import { BusinessSubscriptionService } from '../../../services/business-subscription-service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, RouterOutlet, TranslocoDirective],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);
  private languageService = inject(LanguageService);
  private subscriptionService = inject(BusinessSubscriptionService);

  lang = this.languageService.currentLang;

  readonly dir = computed(() => (this.lang() === 'ur' ? 'rtl' : 'ltr'));

  email = '';
  password = '';
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  showPassword = signal(false);

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }


  login(): void {
    if (!this.email || !this.password) {
      this.errorMessage.set('Please enter both email and password.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.isLoading.set(false);
        const role = this.authService.getRole();
        this.subscriptionService.ensureLoaded();
        this.router.navigate([this.dashboardRouteForRole(role)]);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(
          err.status === 401
            ? 'Invalid email or password.'
            : 'Something went wrong. Please try again.',
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
