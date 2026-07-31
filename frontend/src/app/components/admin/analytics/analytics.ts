import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  ViewChild,
  AfterViewInit,
  inject,
  effect,
  computed,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Chart, registerables } from 'chart.js';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { badgeTypes } from '../../../core/enums/badgeTypes';
import { ChartService } from '../../../services/chart';
import { PlatformReportService } from '../../../services/platform-report-service';
import { ReportPeriod } from '../../../core/enums/report-period';

Chart.register(...registerables);

type GrowthMetric = 'Revenue' | 'Users' | 'Businesses';

const PALETTE = ['#6366f1', '#f59e0b', '#8b5cf6', '#10b981', '#f97316', '#ef4444', '#0ea5e9'];

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule, StatCard],
  templateUrl: './analytics.html',
  styleUrl: './analytics.css',
})
export class Analytics implements AfterViewInit {
  @ViewChild('platformChart') platformRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('revenueChart') revenueRef!: ElementRef<HTMLCanvasElement>;

  chartService = inject(ChartService);
  private reportService = inject(PlatformReportService);

  readonly ReportPeriod = ReportPeriod;

  loading = this.reportService.loading;
  error = this.reportService.error;
  growth = this.reportService.growth;
  revenueByPlan = this.reportService.revenueByPlan;
  topBusinessesRaw = this.reportService.topBusinesses;
  recentActivityRaw = this.reportService.recentActivity;

  selectedPeriod = signal<ReportPeriod>(ReportPeriod.Month);
  activeTab: GrowthMetric = 'Revenue';
  chartTabs: GrowthMetric[] = ['Revenue', 'Users', 'Businesses'];

  private viewReady = false;

  constructor() {
    this.reportService.loadPlatformReport(this.selectedPeriod());

    effect(() => {
      const g = this.growth();
      const plans = this.revenueByPlan();
      if (this.viewReady && g) {
        queueMicrotask(() => this.createCharts());
      }
      void plans;
    });
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    if (this.growth()) {
      this.createCharts();
    }
  }

  stats = computed(() => {
    const g = this.growth();
    if (!g || g.labels.length === 0) return [];

    const last = g.labels.length - 1;
    const prev = last - 1;

    const revenueNow = g.revenue[last] ?? 0;
    const usersNow = g.users[last] ?? 0;
    const businessesNow = g.businesses[last] ?? 0;
    const arpu = usersNow > 0 ? revenueNow / usersNow : 0;

    const delta = (arr: number[]) => {
      if (prev < 0) return null;
      const before = arr[prev] ?? 0;
      const now = arr[last] ?? 0;
      if (before === 0) return now > 0 ? 100 : 0;
      return ((now - before) / before) * 100;
    };

    const badgeFor = (pct: number | null, suffix: string) => {
      if (pct === null) return { text: suffix, type: badgeTypes[2] };
      const rounded = Math.abs(pct).toFixed(0);
      if (pct > 0) return { text: `+${rounded}% vs last period`, type: badgeTypes[0] };
      if (pct < 0) return { text: `-${rounded}% vs last period`, type: badgeTypes[1] };
      return { text: `No change`, type: badgeTypes[2] };
    };

    const revenueBadge = badgeFor(delta(g.revenue), 'This period');
    const usersBadge = badgeFor(delta(g.users), 'This period');
    const businessesBadge = badgeFor(delta(g.businesses), 'This period');

    return [
      {
        icon: 'fa-solid fa-sack-dollar',
        iconBg: 'bg-amber-50',
        iconColor: 'text-amber-500',
        title: `Platform Revenue (${g.labels[last]})`,
        value: this.formatCurrency(revenueNow),
        badgeText: revenueBadge.text,
        badgeType: revenueBadge.type,
      },
      {
        icon: 'fa-solid fa-building',
        iconBg: 'bg-indigo-50',
        iconColor: 'text-indigo-500',
        title: 'New Businesses',
        value: businessesNow.toString(),
        badgeText: businessesBadge.text,
        badgeType: businessesBadge.type,
      },
      {
        icon: 'fa-solid fa-users',
        iconBg: 'bg-teal-50',
        iconColor: 'text-teal-500',
        title: 'New Users',
        value: usersNow.toString(),
        badgeText: usersBadge.text,
        badgeType: usersBadge.type,
      },
      {
        icon: 'fa-solid fa-chart-line',
        iconBg: 'bg-red-50',
        iconColor: 'text-red-400',
        title: 'ARPU',
        value: this.formatCurrency(arpu),
        badgeText: 'Revenue / active user',
        badgeType: badgeTypes[2],
      },
    ];
  });

  revenueBreakdown = computed(() => {
    const plans = this.revenueByPlan();
    return plans.map((p, i) => ({
      label: p.planName,
      value: this.formatCurrency(p.revenue),
      pct: Math.round(p.percentageOfTotal),
      color: PALETTE[i % PALETTE.length],
    }));
  });

  totalPlanRevenueRaw = computed(() =>
    this.revenueByPlan().reduce((sum, p) => sum + p.revenue, 0)
  );

  totalPlanRevenue = computed(() => this.formatCurrency(this.totalPlanRevenueRaw()));

  topBusinesses = computed(() =>
    this.topBusinessesRaw().map((b, i) => ({
      rank: i + 1,
      businessId: b.businessId,
      name: b.businessName,
      revenue: this.formatCurrency(b.revenue),
      pct: Math.round(b.percentageOfTop),
      plan: b.planName,
      color: PALETTE[i % PALETTE.length],
    }))
  );

  recentActivity = computed(() =>
    this.recentActivityRaw().map((event) => ({
      id: event.id,
      message: event.message,
      time: this.formatRelativeTime(event.timestamp),
      ...this.iconFor(event.type),
    }))
  );

  createCharts(): void {
    if (!this.platformRef || !this.revenueRef) return;

    const platformCtx = this.platformRef.nativeElement.getContext('2d')!;
    const revenueCtx = this.revenueRef.nativeElement.getContext('2d')!;

    const g = this.growth();
    if (!g) return;

    const data =
      this.activeTab === 'Revenue' ? g.revenue : this.activeTab === 'Users' ? g.users : g.businesses;

    this.chartService.createLineChart(platformCtx, g.labels, data);

    const plans = this.revenueBreakdown();
    this.chartService.createDoughnutChart(
      revenueCtx,
      plans.map((p) => p.label),
      plans.map((p) => p.pct),
      plans.map((p) => p.color)
    );
  }

  switchTab(tab: GrowthMetric): void {
    this.activeTab = tab;
    this.createCharts();
  }

  onPeriodChange(period: ReportPeriod): void {
    this.selectedPeriod.set(period);
    this.reportService.loadPlatformReport(period);
  }

  exportReport(): void {
    console.log('Export analytics report');
  }


  private formatCurrency(value: number): string {
    if (value >= 10000000) return `Rs ${(value / 10000000).toFixed(2)} Cr`;
    if (value >= 100000) return `Rs ${(value / 100000).toFixed(2)} L`;
    return `Rs ${value.toLocaleString('en-PK')}`;
  }

  private formatRelativeTime(iso: string): string {
    const then = new Date(iso).getTime();
    const diffMs = Date.now() - then;
    const mins = Math.floor(diffMs / 60000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins} min ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs} hr ago`;
    const days = Math.floor(hrs / 24);
    if (days === 1) return 'Yesterday';
    return `${days} days ago`;
  }

  private iconFor(type: string): { icon: string; iconBg: string; iconColor: string } {
    const map: Record<string, { icon: string; iconBg: string; iconColor: string }> = {
      UserSignup: { icon: 'fa-solid fa-user-plus', iconBg: 'bg-indigo-50', iconColor: 'text-indigo-500' },
      SubscriptionUpgraded: { icon: 'fa-solid fa-arrow-up', iconBg: 'bg-amber-50', iconColor: 'text-amber-500' },
      SubscriptionCancelled: { icon: 'fa-solid fa-circle-xmark', iconBg: 'bg-red-50', iconColor: 'text-red-400' },
      SubscriptionRenewed: { icon: 'fa-solid fa-rotate-right', iconBg: 'bg-green-50', iconColor: 'text-green-500' },
      BusinessRegistered: { icon: 'fa-solid fa-building', iconBg: 'bg-indigo-50', iconColor: 'text-indigo-500' },
    };
    return map[type] ?? { icon: 'fa-solid fa-bolt', iconBg: 'bg-hover-bg', iconColor: 'text-text-muted' };
  }
}