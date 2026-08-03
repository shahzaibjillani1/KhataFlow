import { Injectable } from '@angular/core';
import {
  Chart,
  ChartConfiguration,
  ChartOptions,
  registerables,
} from 'chart.js';

Chart.register(...registerables);

@Injectable({
  providedIn: 'root',
})
export class ChartService {

  // ✅ BAR CHART
  createBarChart(ctx: CanvasRenderingContext2D, labels: string[], data: number[]): Chart<'bar'> {
    this.destroyExisting(ctx);

    return new Chart(ctx, {
      type: 'bar',
      data: {
        labels,
        datasets: [
          {
            data,
            backgroundColor: 'rgba(99, 102, 241, 0.12)',
            borderColor: 'rgb(99, 102, 241)',
            borderWidth: 2,
            borderRadius: 8,
          },
        ],
      },
      options: this.getCommonOptions(),
    } as ChartConfiguration<'bar'>);
  }

  createLineChart(ctx: CanvasRenderingContext2D, labels: string[], data: number[]): Chart<'line'> {
    this.destroyExisting(ctx);

    return new Chart(ctx, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            data,
            borderColor: 'rgb(99, 102, 241)',
            tension: 0.4,
            fill: true,
          },
        ],
      },
      options: this.getCommonOptions(),
    } as ChartConfiguration<'line'>);
  }

  createDoughnutChart(
    ctx: CanvasRenderingContext2D,
    labels: string[],
    data: number[],
    colors: string[]
  ): Chart<'doughnut'> {
    this.destroyExisting(ctx);

    const total = data.reduce((sum, v) => sum + v, 0);

    const hasData = total > 0;
    const chartLabels = hasData ? labels : ['No data'];
    const chartData = hasData ? data : [1];
    const chartColors = hasData ? colors : ['#e5e7eb'];

    return new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: chartLabels,
        datasets: [
          {
            data: chartData,
            backgroundColor: chartColors,
            borderWidth: 0,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: { enabled: hasData },
        },
      },
    } as ChartConfiguration<'doughnut'>);
  }

  private destroyExisting(ctx: CanvasRenderingContext2D): void {
    const existing = Chart.getChart(ctx.canvas);
    existing?.destroy();
  }

  private getCommonOptions(): ChartOptions {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            label: (ctx: any) => {
              const value = ctx.raw ?? ctx.parsed?.y ?? 0;
              return `Rs ${value.toLocaleString()}`;
            },
          },
        },
      },
    };
  }
}