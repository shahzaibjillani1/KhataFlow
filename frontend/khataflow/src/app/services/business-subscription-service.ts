import { Injectable, inject, signal, computed } from '@angular/core';
import { BusinessService } from './business-service';
import { AuthService } from './auth-service';

@Injectable({ providedIn: 'root' })
export class BusinessSubscriptionService {
  private businessService = inject(BusinessService);
  private authService = inject(AuthService);

  private readonly _plan = signal<number | null>(null);
  readonly isPremium = computed(() => this._plan() === 1);

  readonly loaded = computed(() => this._plan() !== null);

  private loading = false;

  ensureLoaded(): void {
    if (this.loaded() || this.loading) return;
    const businessId = this.authService.getCurrentBusinessId();
    if (!businessId) return;

    this.loading = true;
    this.businessService.getById(businessId).subscribe({
      next: (res) => {
        this._plan.set(res.data.plan);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  refresh(): void {
    this._plan.set(null);
    this.ensureLoaded();
  }
}
