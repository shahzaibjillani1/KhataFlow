import { AfterViewInit, Component, ElementRef, inject, OnDestroy, signal, ViewChild } from '@angular/core';
import { Chart, registerables } from 'chart.js';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { ChartService } from '../../../services/chart';
import { DashboardService } from '../../../services/dashboard-service';
import { SaleService } from '../../../services/sale-service';
import { ProductService } from '../../../services/product-service';
import { DashboardSummary } from '../../../services/dashboard-models';
import { Sale } from '../../../core/models/sale-models';
import { Product } from '../../../core/models/product-models';
import { LocalizedTextPipe } from '../../../shared/pipes/localized-text-pipe';
import { LanguageService } from '../../../services/language-service';
import { AuthService } from '../../../services/auth-service';

Chart.register(...registerables);

@Component({
  selector: 'app-shop-owner-dashboard',
  standalone: true,
  imports: [CommonModule, StatCard, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './shop-owner-dashboard.html',
  styleUrl: './shop-owner-dashboard.css',
})
export class ShopOwnerDashboard implements AfterViewInit, OnDestroy {
  @ViewChild('salesChart') chartRef!: ElementRef;
  chart!: Chart;

  private router = inject(Router);
  private chartService = inject(ChartService);
  private dashboardService = inject(DashboardService);
  private saleService = inject(SaleService);
  private productService = inject(ProductService);
  private translocoService = inject(TranslocoService);
  private languageService = inject(LanguageService);
  private authService = inject(AuthService);
  protected readonly Math = Math;

  lang = this.languageService.currentLang;

  readonly role = this.authService.getRole();
  readonly isStaff = this.role === 'Staff';
  readonly canSeeFinancials = this.role === 'Owner' || this.role === 'Manager';

  chartType: 'bar' | 'line' = 'bar';

  summary = signal<DashboardSummary | null>(null);
  recentSales = signal<Sale[]>([]);
  topProducts = signal<Product[]>([]);
  isLoadingSummary = signal(true);
  isLoadingChart = signal(true);

  private weeklySalesData: { label: string; amount: number }[] = [];

  private allQuickActions = [
    { labelKey: 'dashboard.quickActions.newSale', icon: 'fa-cart-plus', path: 'sales', primary: true, roles: ['Owner', 'Manager', 'Staff'] },
    { labelKey: 'dashboard.quickActions.addProduct', icon: 'fa-box-open', path: 'products', roles: ['Owner', 'Manager'] },
    { labelKey: 'dashboard.quickActions.addCustomer', icon: 'fa-user-plus', path: 'customers', roles: ['Owner', 'Manager', 'Staff'] },
    { labelKey: 'dashboard.quickActions.newInvoice', icon: 'fa-file-invoice', path: 'invoice', roles: ['Owner', 'Manager', 'Staff'] },
  ];

  readonly quickActions = this.allQuickActions.filter((a) => a.roles.includes(this.role as string));

  ngAfterViewInit(): void {
    this.loadSummary();
    if (this.canSeeFinancials) {
      this.loadWeeklySalesAndRenderChart();
    } else {
      this.isLoadingChart.set(false);
    }
    this.loadRecentSales();
    if (this.canSeeFinancials) {
      this.loadTopProducts();
    }
  }

  private loadSummary() {
    this.dashboardService.fetchSummary().subscribe({
      next: (res) => {
        this.summary.set(res.data);
        this.isLoadingSummary.set(false);
      },
      error: (err) => {
        console.error('Failed to load dashboard summary', err);
        this.isLoadingSummary.set(false);
      },
    });
  }

  private loadWeeklySalesAndRenderChart() {
    this.saleService.getWeeklySales().subscribe({
      next: (res) => {
        this.weeklySalesData = res.data.map((d) => ({ label: d.day, amount: d.totalSales }));
        this.isLoadingChart.set(false);
        this.createChart();
      },
      error: (err) => {
        console.error('Failed to load weekly sales', err);
        this.isLoadingChart.set(false);
      },
    });
  }

  private loadRecentSales() {
    this.saleService.fetchAll().subscribe({
      next: (res) => this.recentSales.set(res.data.items.slice(0, 5)),
      error: (err) => console.error('Failed to load recent sales', err),
    });
  }

  private loadTopProducts() {
    this.productService.getTopSales().subscribe({
      next: (res) => this.topProducts.set(res.data),
      error: (err) => console.error('Failed to load top products', err),
    });
  }

  createChart() {
    const ctx = this.chartRef.nativeElement.getContext('2d')!;
    const labels = this.weeklySalesData.map((d) => d.label);
    const data = this.weeklySalesData.map((d) => d.amount);

    this.chart?.destroy();

    this.chart =
      this.chartType === 'bar'
        ? this.chartService.createBarChart(ctx, labels, data)
        : this.chartService.createLineChart(ctx, labels, data);
  }

  setChartType(type: 'bar' | 'line') {
    this.chartType = type;
    if (this.weeklySalesData.length) {
      this.createChart();
    }
  }

  navigateTo(path: string) {
    this.router.navigate([`/shop-owner-dashboard/${path}`]);
  }

  paymentStatusDisplay(status: number): string {
    switch (status) {
      case 1:
        return this.translocoService.translate('dashboard.status.paid');
      case 0:
        return this.translocoService.translate('dashboard.status.udhar');
      default:
        return this.translocoService.translate('dashboard.status.unknown', { status });
    }
  }

  paymentStatusClass(status: number): string {
    return status === 1
      ? 'bg-badge-paid-bg text-badge-paid-text'
      : 'bg-badge-udhar-bg text-badge-udhar-text';
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }
}