import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, switchMap } from 'rxjs';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { UserService } from '../../../services/user-service';
import { BusinessService } from '../../../services/business-service';
import { AuthService } from '../../../services/auth-service';
import { LanguageService } from '../../../services/language-service';
import { SubscriptionCheckoutService } from '../../../services/subscription-checkout-service';
import { SafepayWebhookService } from '../../../services/safepay-webhook-service';
import { SubscriptionPlanService } from '../../../services/subscription-plan-service';
import { SubscriptionPlan } from '../../../core/models/subscription-plan-models';

interface StatusBanner {
  type: 'success' | 'error';
  text: string;
}

@Component({
  selector: 'app-settings',
  imports: [CommonModule, FormsModule, TranslocoDirective],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings implements OnInit {
  private userService = inject(UserService);
  private businessService = inject(BusinessService);
  private authService = inject(AuthService);
  private translocoService = inject(TranslocoService);
  private languageService = inject(LanguageService);
  private checkoutService = inject(SubscriptionCheckoutService);
  private safepayService = inject(SafepayWebhookService);
  private planService = inject(SubscriptionPlanService);

  readonly lang = this.languageService.currentLang;

  showPassword = false;
  savedBusiness = false;
  savedProfile = false;
  savingBusiness = signal(false);
  savingProfile = signal(false);
  businessError = signal<string | null>(null);
  profileError = signal<string | null>(null);
  loadError = signal<string | null>(null);
  loading = signal(true);

  subscriptionStatus = signal<StatusBanner | null>(null);

  private userId!: string;
  private businessId!: string;

  readonly role = this.authService.getRole();
  readonly isStaff = this.role === 'Staff';
  readonly canSeeOwnerOrManager = this.role === 'Owner' || this.role === 'Manager';
  readonly canSeeOwner = this.role === 'Owner';

  premiumPlan = signal<SubscriptionPlan | null>(null);
  loadingPlan = signal(false);
  planError = signal<string | null>(null);

  premiumFeaturesDisplay(): string[] {
    const plan = this.premiumPlan();
    if (!plan) return [];
    return this.lang() === 'ur' && plan.featuresUr.length ? plan.featuresUr : plan.features;
  }

  business = {
    name: '',
    nameUr: '',
    ownerName: '',
    ownerNameUr: '',
    phone: '',
    address: '',
    addressUr: '',
  };

  profile = {
    email: '',
    newPassword: '',
  };

  preferences = {
    currency: 'PKR',
    lowStockAlert: true,
    whatsappReceipts: true,
    dailySummary: false,
  };

  subscription = {
    plan: 'Basic Plan',
    renewsOn: '',
    locationsUsed: 1,
    locationsTotal: 1,
  };

  ngOnInit(): void {
    this.consumeSafepayReturn();

    const currentUserId = this.authService.getCurrentUserId();

    if (!currentUserId) {
      this.loading.set(false);
      this.loadError.set('No authenticated user found.');
      return;
    }

    this.userId = currentUserId;

    this.userService
      .getUserById(this.userId)
      .pipe(
        switchMap((userRes) => {
          const u = userRes.data;

          this.businessId = u.businessId;
          this.profile.email = u.email ?? '';
          this.business.ownerName = u.fullName ?? '';
          this.business.ownerNameUr = u.fullNameUr ?? '';

          return this.businessService.getById(this.businessId);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (businessRes) => {
          const b = businessRes.data;
          this.business.name = b.name;
          this.business.nameUr = b.nameUr ?? '';
          this.business.phone = b.phoneNumber;
          this.business.address = b.address ?? '';
          this.business.addressUr = b.addressUr ?? '';
          this.subscription.plan = b.plan === 1 ? 'Premium Plan' : 'Basic Plan';
          this.subscription.renewsOn = new Date(b.subscriptionExpiry).toLocaleDateString('en-PK', {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
          });
        },
        error: () => this.loadError.set('Could not load your account or business info.'),
      });
  }

  private consumeSafepayReturn(): void {
    const result = this.safepayService.consumeReturnParams();
    if (!result) return;

    if (result.status === 'success') {
      this.subscriptionStatus.set({
        type: 'success',
        text: result.message ?? "You're now on the Premium Plan.",
      });
    } else {
      this.subscriptionStatus.set({
        type: 'error',
        text:
          result.message ??
          (result.status === 'cancelled'
            ? 'Checkout was cancelled — you have not been charged.'
            : 'Payment could not be completed. You have not been charged.'),
      });
    }
  }

  dismissSubscriptionStatus(): void {
    this.subscriptionStatus.set(null);
  }

  businessNameDisplay(): string {
    return this.lang() === 'ur' ? this.business.nameUr : this.business.name;
  }
  businessAddressDisplay(): string {
    return this.lang() === 'ur' ? this.business.addressUr : this.business.address;
  }

  businessOwnerNameDisplay(): string {
    return this.lang() === 'ur' ? this.business.ownerNameUr : this.business.ownerName;
  }

  saveBusinessInfo(): void {
    this.businessError.set(null);
    this.savingBusiness.set(true);

    this.businessService
      .update(this.businessId, {
        id: this.businessId,
        name: this.business.name,
        email: this.profile.email,
        phoneNumber: this.business.phone,
        address: this.business.address,
      })
      .pipe(finalize(() => this.savingBusiness.set(false)))
      .subscribe({
        next: () => {
          this.savedBusiness = true;
          setTimeout(() => (this.savedBusiness = false), 2000);
        },
        error: (err) =>
          this.businessError.set(err?.error?.message ?? 'Failed to save business info.'),
      });

    this.userService.updateUser(this.userId, { fullName: this.business.ownerName }).subscribe();
  }

  updateProfile(): void {
    this.profileError.set(null);
    this.savingProfile.set(true);

    this.userService
      .updateUser(this.userId, { email: this.profile.email })
      .pipe(finalize(() => this.savingProfile.set(false)))
      .subscribe({
        next: () => {
          this.savedProfile = true;
          this.profile.newPassword = '';
          setTimeout(() => (this.savedProfile = false), 2000);
        },
        error: (err) => this.profileError.set(err?.error?.message ?? 'Failed to update profile.'),
      });
  }

  onLanguageChange(lang: string): void {
    this.translocoService.setActiveLang(lang);
    localStorage.setItem('lang', lang);
  }

  planKey(): string {
    return this.subscription.plan === 'Premium Plan'
      ? 'settings.subscription.premiumPlan'
      : 'settings.subscription.basicPlan';
  }

  showUpgradeModal = false;
  startingCheckout = signal(false);
  checkoutError = signal<string | null>(null);

  openUpgradeModal(): void {
    this.showUpgradeModal = true;
    this.startingCheckout.set(false);
    this.checkoutError.set(null);
    this.loadPremiumPlan();
  }

  closeUpgradeModal(): void {
    if (this.startingCheckout()) return;
    this.showUpgradeModal = false;
  }

  private loadPremiumPlan(): void {
    const cached = this.planService.plans();
    if (cached.length) {
      this.selectPremiumFrom(cached);
      return;
    }

    this.loadingPlan.set(true);
    this.planError.set(null);

    this.planService
      .fetchAll()
      .pipe(finalize(() => this.loadingPlan.set(false)))
      .subscribe({
        next: (res) => this.selectPremiumFrom(res.data),
        error: () => this.planError.set('Could not load plan details.'),
      });
  }

  private selectPremiumFrom(plans: SubscriptionPlan[]): void {
    const premium = plans.find((p) => p.planType === 1 && p.isActive);
    if (premium) {
      this.premiumPlan.set(premium);
    } else {
      this.planError.set('Premium plan is not available right now.');
    }
  }

  startCheckout(): void {
    const plan = this.premiumPlan();
    if (!plan) return;

    this.checkoutError.set(null);
    this.startingCheckout.set(true);

    this.checkoutService.startCheckout(plan.id).subscribe({
      next: (res) => {
        if (res.result && res.data?.checkoutUrl) {
          window.location.href = res.data.checkoutUrl;
        } else {
          this.startingCheckout.set(false);
          this.checkoutError.set(res.message || 'Could not start checkout.');
        }
      },
      error: (err) => {
        this.startingCheckout.set(false);
        this.checkoutError.set(
          err?.error?.message ?? 'Could not start checkout. Please try again.',
        );
      },
    });
  }

  upgradePlan(): void {
    this.openUpgradeModal();
  }
}