import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { Business } from '../../../core/models/business-models';
import { BusinessStatus } from '../../../core/enums/business-status';
import { BusinessPlanType } from '../../../core/enums/business-plan-type';
import { BusinessService } from '../../../services/business-service';


const STATUS_LABELS: Record<number, string> = {
  [BusinessStatus.Active]: 'Active',
  [BusinessStatus.Trial]: 'Trial',
  [BusinessStatus.Suspended]: 'Suspended',
};

const PLAN_LABELS: Record<number, string> = {
  [BusinessPlanType.Free]: 'Free',
  [BusinessPlanType.Premium]: 'Premium',
};

const AVATAR_COLORS = ['#6366f1', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#f97316'];

interface BusinessViewModel {
  id: string;
  name: string;
  category: string; // not in API — placeholder until backend adds it
  owner: string; // API has no owner name — falling back to email
  phone: string;
  plan: string;
  planCode: number;
  status: string;
  statusCode: number;
  joined: string;
  joinedAgo: string;
  avatarColor: string;
}

function relativeTime(iso: string): string {
  const then = new Date(iso).getTime();
  const diffMs = Date.now() - then;
  const days = Math.floor(diffMs / 86_400_000);

  if (days <= 0) return 'today';
  if (days === 1) return '1 day ago';
  if (days < 7) return `${days} days ago`;
  if (days < 30) return `${Math.floor(days / 7)} week${Math.floor(days / 7) > 1 ? 's' : ''} ago`;
  if (days < 365) return `${Math.floor(days / 30)} month${Math.floor(days / 30) > 1 ? 's' : ''} ago`;
  return `${Math.floor(days / 365)} year${Math.floor(days / 365) > 1 ? 's' : ''} ago`;
}

function avatarColorFor(id: string): string {
  const hash = [...id].reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
  return AVATAR_COLORS[hash % AVATAR_COLORS.length];
}

function toViewModel(b: Business): BusinessViewModel {
  return {
    id: b.id,
    name: b.name,
    category: '—', // TODO: backend has no category field on Business yet
    owner: b.email, // TODO: backend has no ownerName on Business yet — using email as stand-in
    phone: b.phoneNumber,
    plan: PLAN_LABELS[b.plan] ?? 'Unknown',
    planCode: b.plan,
    status: STATUS_LABELS[b.status] ?? 'Unknown',
    statusCode: b.status,
    joined: new Date(b.registeredAt).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }),
    joinedAgo: relativeTime(b.registeredAt),
    avatarColor: avatarColorFor(b.id),
  };
}

@Component({
  selector: 'app-businesses',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './businesses.html',
  styleUrl: './businesses.css',
})
export class Businesses implements OnInit {
  private businessService = inject(BusinessService);

  search = '';
  selectedPlan = '';
  selectedStatus = '';

  loading = signal(true);
  error = signal<string | null>(null);
  actionPending = signal<string | null>(null); 

  businesses = computed(() => this.businessService.businesses().map(toViewModel));

  showSuspendModal = false;
  suspendingBiz: BusinessViewModel | null = null;
  suspendReason = '';

  showActivateModal = false;
  activatingBiz: BusinessViewModel | null = null;

  showUpgradeModal = false;
  upgradingBiz: BusinessViewModel | null = null;
  upgradeTargetPlan = '';
  upgradeExpiryDate = '';

  showDetailsModal = false;
  detailsBiz: BusinessViewModel | null = null;

  showLoginModal = false;
  loginBiz: BusinessViewModel | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.businessService
      .fetchAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        error: (err) => {
          console.error('[Businesses] Failed to load businesses', err);
          this.error.set('Could not load businesses.');
        },
      });
  }

  get filteredBusinesses(): BusinessViewModel[] {
    const term = this.search.toLowerCase();
    return this.businesses().filter((b) => {
      const matchSearch =
        b.name.toLowerCase().includes(term) || b.owner.toLowerCase().includes(term);
      const matchPlan = this.selectedPlan ? b.plan === this.selectedPlan : true;
      const matchStatus = this.selectedStatus ? b.status === this.selectedStatus : true;
      return matchSearch && matchPlan && matchStatus;
    });
  }

  get activeCount() {
    return this.businesses().filter((b) => b.status === 'Active').length;
  }
  get trialCount() {
    return this.businesses().filter((b) => b.status === 'Trial').length;
  }
  get suspendedCount() {
    return this.businesses().filter((b) => b.status === 'Suspended').length;
  }

  nextPlan(plan: string): string {
    return 'Premium';
  }

  private nextPlanCode(planCode: number): number {
    return BusinessPlanType.Premium;
  }


  loginAs(biz: BusinessViewModel) {
    this.loginBiz = biz;
    this.showLoginModal = true;
  }

  confirmLoginAs() {
    if (!this.loginBiz) return;
    const id = this.loginBiz.id;
    this.actionPending.set(id);

    this.businessService
      .getImpersonationToken(id)
      .pipe(finalize(() => this.actionPending.set(null)))
      .subscribe({
        next: (res) => {
          console.log('Impersonation token acquired for', this.loginBiz?.name, res.data.accessToken);
          this.showLoginModal = false;
        },
        error: (err) => {
          console.error('[Businesses] Failed to get impersonation token', err);
          this.error.set('Could not log in as this business.');
        },
      });
  }

  suspend(biz: BusinessViewModel) {
    this.suspendingBiz = biz;
    this.suspendReason = '';
    this.showSuspendModal = true;
  }

  confirmSuspend() {
    if (!this.suspendingBiz) return;
    const id = this.suspendingBiz.id;
    this.actionPending.set(id);

    this.businessService
      .suspend(id, this.suspendReason)
      .pipe(finalize(() => this.actionPending.set(null)))
      .subscribe({
        next: () => {
          this.showSuspendModal = false;
          this.load(); 
        },
        error: (err) => {
          console.error('[Businesses] Failed to suspend business', err);
          this.error.set('Could not suspend this business.');
        },
      });
  }

  activate(biz: BusinessViewModel) {
    this.activatingBiz = biz;
    this.showActivateModal = true;
  }

  confirmActivate() {
    if (!this.activatingBiz) return;
    const id = this.activatingBiz.id;
    this.actionPending.set(id);

    this.businessService
      .reactivate(id)
      .pipe(finalize(() => this.actionPending.set(null)))
      .subscribe({
        next: () => {
          this.showActivateModal = false;
          this.load();
        },
        error: (err) => {
          console.error('[Businesses] Failed to reactivate business', err);
          this.error.set('Could not activate this business.');
        },
      });
  }

  upgrade(biz: BusinessViewModel) {
    this.upgradingBiz = biz;
    this.upgradeTargetPlan = this.nextPlan(biz.plan);
    const d = new Date();
    d.setDate(d.getDate() + 30);
    this.upgradeExpiryDate = d.toISOString().slice(0, 10);
    this.showUpgradeModal = true;
  }

  confirmUpgrade() {
    if (!this.upgradingBiz) return;
    const id = this.upgradingBiz.id;
    this.actionPending.set(id);

    this.businessService
      .changeSubscription(id, {
        newPlan: this.nextPlanCode(this.upgradingBiz.planCode),
        customExpiryDate: new Date(this.upgradeExpiryDate).toISOString(),
      })
      .pipe(finalize(() => this.actionPending.set(null)))
      .subscribe({
        next: () => {
          this.showUpgradeModal = false;
          this.load();
        },
        error: (err) => {
          console.error('[Businesses] Failed to upgrade business plan', err);
          this.error.set('Could not upgrade this business.');
        },
      });
  }

  viewDetails(biz: BusinessViewModel) {
    this.detailsBiz = biz;
    this.showDetailsModal = true;
  }

  exportData() {
    const rows = [
      'Business,Owner,Phone,Plan,Status,Joined',
      ...this.businesses().map(
        (b) => `${b.name},${b.owner},${b.phone},${b.plan},${b.status},${b.joined}`
      ),
    ].join('\n');
    const blob = new Blob([rows], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'businesses.csv';
    a.click();
    URL.revokeObjectURL(url);
  }
}