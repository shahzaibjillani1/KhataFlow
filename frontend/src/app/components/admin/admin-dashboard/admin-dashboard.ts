import {
  Component,
  AfterViewInit,
  ViewChild,
  ElementRef,
  inject,
  signal,
  computed,
  effect,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin, of } from 'rxjs';
import { catchError, switchMap, map } from 'rxjs/operators';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { badgeTypes } from '../../../core/enums/badgeTypes';
import { ChartService } from '../../../services/chart';
import { PlatformSummary } from '../../../core/models/business-models';
import { BusinessService } from '../../../services/business-service';
import { SubscriptionPlanService } from '../../../services/subscription-plan-service';
import { UserService } from '../../../services/user-service';
import { User } from '../../../core/models/user-model';

interface PlanDistributionItem {
  id: string;
  label: string;
  value: number;
  color: string;
}

const PLAN_COLORS = ['#6b7280', '#6366f1', '#eab308', '#10b981', '#f97316', '#ec4899'];

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, StatCard],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css',
})
export class AdminDashboard implements AfterViewInit {
  @ViewChild('userGrowthChart') userGrowthRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('planChart') planChartRef!: ElementRef<HTMLCanvasElement>;

  private chartService = inject(ChartService);
  private businessService = inject(BusinessService);
  private subscriptionPlanService = inject(SubscriptionPlanService);
  private userService = inject(UserService);

  private viewReady = false;

  loading = signal(true);
  error = signal<string | null>(null);

  summary = signal<PlatformSummary | null>(null);
  plans = signal<PlanDistributionItem[]>([]);

  userGrowthData = signal<{ labels: string[]; values: number[] }>({
    labels: [],
    values: [],
  });

  stats = computed(() => {
    const s = this.summary();
    return [
      {
        icon: 'fa-solid fa-users',
        iconBg: 'bg-indigo-100',
        iconColor: 'text-indigo-500',
        title: 'Total Users',
        value: s ? s.totalUsers.toLocaleString() : '—',
        badgeText: s ? `${s.newThisWeek} today` : '',
        badgeType: badgeTypes[0],
      },
      {
        icon: 'fa-solid fa-circle-check',
        iconBg: 'bg-emerald-100',
        iconColor: 'text-emerald-500',
        title: 'Active Subscriptions',
        value: s ? s.activeSubscriptions.toLocaleString() : '—',
        badgeText:
          s && s.totalUsers > 0
            ? `${Math.round((s.activeSubscriptions / s.totalUsers) * 100)}% of users`
            : '',
        badgeType: badgeTypes[0],
      },
      {
        icon: 'fa-solid fa-calendar-days',
        iconBg: 'bg-yellow-100',
        iconColor: 'text-yellow-500',
        title: 'New This Week',
        value: s ? s.newThisWeek.toLocaleString() : '—',
        badgeText: s ? `${s.churnRate}% churn` : '',
        badgeType: badgeTypes[0],
      },
      {
        icon: 'fa-solid fa-sack-dollar',
        iconBg: 'bg-orange-100',
        iconColor: 'text-orange-400',
        title: 'Platform Revenue',
        value: s ? this.formatRevenue(s.platformRevenue) : '—',
        badgeText: s ? `ARPU Rs ${s.arpu.toFixed(0)}` : '',
        badgeType: badgeTypes[0],
      },
    ];
  });

  get totalPlans(): number {
    return this.plans().reduce((sum, p) => sum + p.value, 0);
  }
  private computeUserGrowth(users: User[]): { labels: string[]; values: number[] } {
    const now = new Date();
    const months = Array.from({ length: 6 }, (_, i) => {
      const d = new Date(now.getFullYear(), now.getMonth() - (5 - i), 1);
      return { year: d.getFullYear(), month: d.getMonth() };
    });

    const createdDates = users
      .map((u) => new Date(u.createdAt))
      .sort((a, b) => a.getTime() - b.getTime());

    const values = months.map(({ year, month }) => {
      const endOfMonth = new Date(year, month + 1, 0, 23, 59, 59, 999);
      return createdDates.filter((d) => d <= endOfMonth).length;
    });

    const labels = months.map(({ year, month }) =>
      new Date(year, month, 1).toLocaleString('default', { month: 'short' }),
    );

    return { labels, values };
  }

  constructor() {
    effect(() => {
      const s = this.summary();
      const p = this.plans();
      const g = this.userGrowthData();
      if (this.viewReady && s && p.length && g.values.length) {
        this.createCharts();
      }
    });

    this.loadDashboard();
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    if (this.summary() && this.plans().length && this.userGrowthData().values.length) {
      this.createCharts();
    }
  }

  private createCharts(): void {
    if (!this.userGrowthRef || !this.planChartRef) return;

    const growthCtx = this.userGrowthRef.nativeElement.getContext('2d')!;
    const planCtx = this.planChartRef.nativeElement.getContext('2d')!;

    const growth = this.userGrowthData();
    this.chartService.createBarChart(growthCtx, growth.labels, growth.values);

    this.chartService.createDoughnutChart(
      planCtx,
      this.plans().map((p) => p.label),
      this.plans().map((p) => p.value),
      this.plans().map((p) => p.color),
    );
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      summary: this.businessService.getPlatformSummary(),
      plans: this.subscriptionPlanService.fetchAll(),
      users: this.userService.getUsers(),
    })
      .pipe(
        map(({ summary, plans, users }) => {
          this.summary.set(summary.data);
          this.userGrowthData.set(this.computeUserGrowth(users.data));

          return plans.data.map((plan, i) => ({
            id: plan.id,
            label: plan.planName,
            value: plan.userCount ?? 0,
            color: PLAN_COLORS[i % PLAN_COLORS.length],
          }));
        }),
        catchError((err) => {
          console.error('[AdminDashboard] Failed to load dashboard data', err);
          this.error.set('Could not load dashboard data.');
          return of([] as PlanDistributionItem[]);
        }),
      )
      .subscribe((plans) => {
        this.plans.set(plans);
        this.loading.set(false);
      });
  }

  private formatRevenue(value: number): string {
    if (value >= 100000) {
      return `Rs ${(value / 100000).toFixed(1)}L`;
    }
    return `Rs ${value.toLocaleString()}`;
  }
}
