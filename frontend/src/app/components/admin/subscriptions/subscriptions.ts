import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import {
  SubscriptionPlan,
  SubscriptionPlanUpdateRequest,
} from '../../../core/models/subscription-plan-models';
import { Business } from '../../../core/models/business-models';
import { SubscriptionPlanService } from '../../../services/subscription-plan-service';
import { BusinessService } from '../../../services/business-service';
import { BusinessStatus } from '../../../core/enums/business-status';
import { BusinessPlanType } from '../../../core/enums/business-plan-type';

const PLAN_COLORS = ['#6b7280', '#8b5cf6', '#f59e0b', '#10b981', '#f97316', '#6366f1'];

interface SubActivityViewModel {
  businessId: string;
  business: string;
  owner: string;
  avatarColor: string;
  plan: string;
  planCode: number;
  expiry: string;
  daysLeft: number;
  amount: number;
  status: 'Active' | 'Expired' | 'Suspended';
}

function avatarColorFor(id: string): string {
  const hash = [...id].reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
  return PLAN_COLORS[hash % PLAN_COLORS.length];
}

function daysUntil(iso: string): number {
  const diffMs = new Date(iso).getTime() - Date.now();
  return Math.max(0, Math.ceil(diffMs / 86_400_000));
}

@Component({
  selector: 'app-subscriptions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subscriptions.html',
  styleUrl: './subscriptions.css',
})
export class Subscriptions implements OnInit {
  private subscriptionPlanService = inject(SubscriptionPlanService);
  private businessService = inject(BusinessService);

  filterStatus = '';

  loading = signal(true);
  error = signal<string | null>(null);
  actionPending = signal<string | null>(null);

  plans = this.subscriptionPlanService.plans;

  private businesses = this.businessService.businesses;

  // Pagination state — server-driven via BusinessService
  pageNumber = signal(1);
  pageSize = signal(10);
  readonly pageSizeOptions = [10, 20, 50, 100];

  readonly totalCount = this.businessService.totalCount;
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  activity = computed<SubActivityViewModel[]>(() => {
    const planByType = new Map<number, SubscriptionPlan>(this.plans().map((p) => [p.planType, p]));

    return this.businesses().map((b: Business) => {
      const plan = planByType.get(b.plan);
      const expired = new Date(b.subscriptionExpiry).getTime() < Date.now();

      let status: SubActivityViewModel['status'] = 'Active';
      if (expired) status = 'Expired';
      else if (b.status === BusinessStatus.Suspended) status = 'Suspended';

      return {
        businessId: b.id,
        business: b.name,
        owner: b.email,
        avatarColor: avatarColorFor(b.id),
        plan: plan?.planName ?? 'Unknown',
        planCode: b.plan,
        expiry: new Date(b.subscriptionExpiry).toLocaleDateString('en-US', {
          month: 'short',
          day: 'numeric',
        }),
        daysLeft: expired ? 0 : daysUntil(b.subscriptionExpiry),
        amount: plan?.monthlyPrice ?? 0,
        status,
      };
    });
  });

  showEditModal = false;
  editingPlan: SubscriptionPlan | null = null;
  editForm = { price: 0, features: '' };

  showChangeModal = false;
  changingSub: SubActivityViewModel | null = null;
  selectedPlanId = '';

  showRenewModal = false;
  renewingSub: SubActivityViewModel | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      plans: this.subscriptionPlanService.fetchAll(),
      businesses: this.businessService.fetchAll(this.pageNumber(), this.pageSize()),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        error: (err) => {
          console.error('[Subscriptions] Failed to load data', err);
          this.error.set('Could not load subscription data.');
        },
      });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.pageNumber()) return;
    this.pageNumber.set(page);
    this.load();
  }

  nextPage(): void {
    this.goToPage(this.pageNumber() + 1);
  }

  prevPage(): void {
    this.goToPage(this.pageNumber() - 1);
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.pageNumber.set(1);
    this.load();
  }

  get filteredActivity(): SubActivityViewModel[] {
    return this.activity().filter((s) =>
      this.filterStatus ? s.status === this.filterStatus : true,
    );
  }
  get activeCount() {
    return this.activity().filter((s) => s.status === 'Active').length;
  }
  get expiredCount() {
    return this.activity().filter((s) => s.status === 'Expired').length;
  }
  get suspendedCount() {
    return this.activity().filter((s) => s.status === 'Suspended').length;
  }

  isPopular(plan: SubscriptionPlan): boolean {
    return plan.planType === BusinessPlanType.Premium;
  }

  editPlan(plan: SubscriptionPlan) {
    this.editingPlan = { ...plan };
    this.editForm = {
      price: plan.monthlyPrice,
      features: plan.features.join('\n'),
    };
    this.showEditModal = true;
  }

  saveEditPlan() {
    if (!this.editingPlan) return;
    const id = this.editingPlan.id;
    this.actionPending.set(id);

    const request: SubscriptionPlanUpdateRequest = {
      id,
      planName: this.editingPlan.planName,
      monthlyPrice: this.editForm.price,
      features: this.editForm.features
        .split('\n')
        .map((f) => f.trim())
        .filter(Boolean),
      isActive: this.editingPlan.isActive,
    };

    this.subscriptionPlanService
      .update(id, request)
      .pipe(finalize(() => this.actionPending.set(null)))
      .subscribe({
        next: () => {
          this.showEditModal = false;
        },
        error: (err) => {
          console.error('[Subscriptions] Failed to update plan', err);
          this.error.set('Could not save plan changes.');
        },
      });
  }

  changePlan(sub: SubActivityViewModel) {
    this.changingSub = sub;
    const current = this.plans().find((p) => p.planType === sub.planCode);
    this.selectedPlanId = current?.id ?? '';
    this.showChangeModal = true;
  }

  saveChangePlan() {
    if (!this.changingSub) return;
    const targetPlan = this.plans().find((p) => p.id === this.selectedPlanId);
    if (!targetPlan) return;

    const businessId = this.changingSub.businessId;
    this.actionPending.set(businessId);

    const expiry = new Date();
    expiry.setDate(expiry.getDate() + 30);

    this.businessService
      .changeSubscription(businessId, {
        newPlan: targetPlan.planType,
        customExpiryDate: expiry.toISOString(),
      })
      .pipe(finalize(() => this.actionPending.set(null)))
      .subscribe({
        next: () => {
          this.showChangeModal = false;
          this.load();
        },
        error: (err) => {
          console.error('[Subscriptions] Failed to change plan', err);
          this.error.set('Could not change this subscription plan.');
        },
      });
  }

  renewSub(sub: SubActivityViewModel) {
    this.renewingSub = sub;
    this.showRenewModal = true;
  }

  confirmRenew() {
    if (!this.renewingSub) return;
    const businessId = this.renewingSub.businessId;
    this.actionPending.set(businessId);

    this.businessService
      .renewSubscription(businessId)
      .pipe(finalize(() => this.actionPending.set(null)))
      .subscribe({
        next: () => {
          this.showRenewModal = false;
          this.load();
        },
        error: (err) => {
          console.error('[Subscriptions] Failed to renew subscription', err);
          this.error.set('Could not renew this subscription.');
        },
      });
  }

  exportActivity() {
    const rows = [
      'Business,Owner,Plan,Expiry,Amount,Status',
      ...this.activity().map(
        (s) => `${s.business},${s.owner},${s.plan},${s.expiry},${s.amount},${s.status}`,
      ),
    ].join('\n');
    const blob = new Blob([rows], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'subscriptions.csv';
    a.click();
    URL.revokeObjectURL(url);
  }
}
