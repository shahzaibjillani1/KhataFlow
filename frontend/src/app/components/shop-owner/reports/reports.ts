import { CommonModule } from '@angular/common';
import * as XLSX from 'xlsx';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import {
  AfterViewInit,
  Component,
  ElementRef,
  inject,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { Chart, registerables } from 'chart.js';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ReportService } from '../../../services/report-service';
import { ProductService } from '../../../services/product-service';
import { SaleService } from '../../../services/sale-service';
import { FinancialReport } from '../../../core/models/report-models';
import { MonthlyRevenue } from '../../../core/models/sale-models';
import { LocalizedTextPipe } from '../../../shared/pipes/localized-text-pipe';
import { LanguageService } from '../../../services/language-service';

Chart.register(...registerables);

interface TopProduct {
  rank: number;
  name: string;
  nameUr: string | null;
  price: number;
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function daysAgoIso(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}

@Component({
  selector: 'app-reports',
  imports: [CommonModule, FormsModule, StatCard, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './reports.html',
  styleUrl: './reports.css',
})
export class Reports implements OnInit, AfterViewInit {
  private reportService = inject(ReportService);
  private productService = inject(ProductService);
  private saleService = inject(SaleService);
  private translocoService = inject(TranslocoService);
  private languageService = inject(LanguageService);

  lang = this.languageService.currentLang;

  @ViewChild('revenueChart') revenueChartRef?: ElementRef<HTMLCanvasElement>;
  private chart?: Chart;

  dateFrom = daysAgoIso(7);
  dateTo = todayIso();

  isLoading = signal(true);
  loadError = signal<string | null>(null);
  topProductsError = signal<string | null>(null);
  revenueTrendError = signal<string | null>(null);

  report = signal<FinancialReport | null>(null);
  topProducts = signal<TopProduct[]>([]);
  revenueData: MonthlyRevenue[] = [];

  get totalRevenue() {
    return this.report()?.totalRevenue ?? 0;
  }
  get grossProfit() {
    return this.report()?.grossProfit ?? 0;
  }
  get totalOrders() {
    return this.report()?.totalOrders ?? 0;
  }
  get avgOrderValue() {
    return this.report()?.averageOrderValue ?? 0;
  }
  get expenses() {
    return this.report()?.totalExpenses ?? 0;
  }
  get totalOutstanding() {
    return this.report()?.totalOutstanding ?? 0;
  }
  get totalCustomers() {
    return this.report()?.totalCustomers ?? 0;
  }
  get isProfitNegative() {
    return this.grossProfit < 0;
  }

  ngOnInit(): void {
    this.loadReport();
  }

  ngAfterViewInit(): void {
    if (this.revenueData.length > 0) {
      this.renderChart();
    }
  }

  private loadReport(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.topProductsError.set(null);
    this.revenueTrendError.set(null);

    const year = new Date(this.dateTo).getFullYear();

    forkJoin({
      report: this.reportService.getFinancialReport(this.dateFrom, this.dateTo),
      topSales: this.productService.getTopSales().pipe(
        catchError((err) => {
          console.error('Failed to load top products', err);
          this.topProductsError.set(this.translocoService.translate('reports.errors.topProducts'));
          return of(null);
        }),
      ),
      monthlyRevenue: this.saleService.getMonthlyRevenue(year).pipe(
        catchError((err) => {
          console.error('Failed to load monthly revenue', err);
          this.revenueTrendError.set(this.translocoService.translate('reports.errors.revenueTrend'));
          return of(null);
        }),
      ),
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ report, topSales, monthlyRevenue }) => {
          this.report.set(report.data);

          this.topProducts.set(
            (topSales?.data ?? []).map((p, i) => ({
              rank: i + 1,
              name: p.productName,
              nameUr: p.productNameUr ?? null,
              price: p.price,
            })),
          );

          this.revenueData = monthlyRevenue?.data ?? [];

          setTimeout(() => this.renderChart());
        },
        error: (err) => {
          console.error('Failed to load report data', err);
          this.loadError.set(this.translocoService.translate('reports.errors.loadFailed'));
        },
      });
  }

  private renderChart(): void {
    const canvas = this.revenueChartRef?.nativeElement;
    if (!canvas || this.revenueData.length === 0) return;

    this.chart?.destroy();

    const revenueLabel = this.translocoService.translate('reports.chart.revenue');

    this.chart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: this.revenueData.map((d) => d.month),
        datasets: [
          {
            label: revenueLabel,
            data: this.revenueData.map((d) => d.totalRevenue),
            backgroundColor: '#5B4FE9',
            borderRadius: 6,
            maxBarThickness: 28,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => `Rs ${Number(ctx.raw).toLocaleString('en-PK')}`,
            },
          },
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: { callback: (v) => `Rs ${Number(v).toLocaleString('en-PK')}` },
          },
        },
      },
    });
  }

  applyFilter(): void {
    if (!this.dateFrom || !this.dateTo) return;
    if (this.dateFrom > this.dateTo) {
      this.loadError.set(this.translocoService.translate('reports.errors.dateRangeInvalid'));
      return;
    }
    this.loadReport();
  }

  exportPDF(): void {
    const rpt = this.report();
    if (!rpt) return;

    const activeLang = this.lang();
    const localize = (en: string, ur: string | null | undefined) =>
      activeLang === 'ur' && ur?.trim() ? ur : en;

    const doc = new jsPDF();

    doc.setFontSize(16);
    doc.setTextColor(91, 79, 233);
    doc.text('KhataFlow — Financial Report', 14, 18);

    doc.setFontSize(10);
    doc.setTextColor(100);
    doc.text(`Period: ${this.dateFrom} to ${this.dateTo}`, 14, 25);

    autoTable(doc, {
      startY: 32,
      head: [['Metric', 'Value']],
      body: [
        ['Total Revenue', `Rs ${rpt.totalRevenue.toLocaleString('en-PK')}`],
        ['Gross Profit', `Rs ${rpt.grossProfit.toLocaleString('en-PK')}`],
        ['Total Expenses', `Rs ${rpt.totalExpenses.toLocaleString('en-PK')}`],
        ['Total Outstanding', `Rs ${rpt.totalOutstanding.toLocaleString('en-PK')}`],
        ['Total Orders', rpt.totalOrders.toString()],
        ['Total Customers', rpt.totalCustomers.toString()],
        ['Average Order Value', `Rs ${rpt.averageOrderValue.toLocaleString('en-PK')}`],
      ],
      theme: 'striped',
      headStyles: { fillColor: [91, 79, 233] },
    });

    if (this.revenueData.length) {
      autoTable(doc, {
        startY: (doc as any).lastAutoTable.finalY + 10,
        head: [['Month', 'Revenue']],
        body: this.revenueData.map((r) => [
          r.month,
          `Rs ${r.totalRevenue.toLocaleString('en-PK')}`,
        ]),
        theme: 'grid',
        headStyles: { fillColor: [91, 79, 233] },
      });
    }

    if (this.topProducts().length) {
      autoTable(doc, {
        startY: (doc as any).lastAutoTable.finalY + 10,
        head: [['Rank', 'Product', 'Price']],
        body: this.topProducts().map((p) => [
          p.rank.toString(),
          localize(p.name, p.nameUr),
          `Rs ${p.price.toLocaleString('en-PK')}`,
        ]),
        theme: 'grid',
        headStyles: { fillColor: [91, 79, 233] },
      });
    }

    doc.save(`khataflow-report-${this.dateFrom}_to_${this.dateTo}.pdf`);
  }

  exportExcel(): void {
    const rpt = this.report();
    if (!rpt) return;

    const activeLang = this.lang();
    const localize = (en: string, ur: string | null | undefined) =>
      activeLang === 'ur' && ur?.trim() ? ur : en;

    const summarySheet = XLSX.utils.json_to_sheet([
      { Metric: 'Date From', Value: this.dateFrom },
      { Metric: 'Date To', Value: this.dateTo },
      { Metric: 'Total Revenue', Value: rpt.totalRevenue },
      { Metric: 'Gross Profit', Value: rpt.grossProfit },
      { Metric: 'Total Expenses', Value: rpt.totalExpenses },
      { Metric: 'Total Outstanding', Value: rpt.totalOutstanding },
      { Metric: 'Total Orders', Value: rpt.totalOrders },
      { Metric: 'Total Customers', Value: rpt.totalCustomers },
      { Metric: 'Average Order Value', Value: rpt.averageOrderValue },
    ]);

    const revenueSheet = XLSX.utils.json_to_sheet(
      this.revenueData.map((r) => ({ Month: r.month, 'Total Revenue': r.totalRevenue })),
    );

    const topProductsSheet = XLSX.utils.json_to_sheet(
      this.topProducts().map((p) => ({
        Rank: p.rank,
        Product: localize(p.name, p.nameUr),
        Price: p.price,
      })),
    );

    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, summarySheet, 'Summary');
    if (this.revenueData.length) {
      XLSX.utils.book_append_sheet(workbook, revenueSheet, 'Revenue Trend');
    }
    if (this.topProducts().length) {
      XLSX.utils.book_append_sheet(workbook, topProductsSheet, 'Top Products');
    }

    XLSX.writeFile(workbook, `khataflow-report-${this.dateFrom}_to_${this.dateTo}.xlsx`);
  }
}