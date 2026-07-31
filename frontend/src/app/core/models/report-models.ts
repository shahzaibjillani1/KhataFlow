export interface FinancialReport {
  from: string;
  to: string;
  totalRevenue: number;
  totalExpenses: number;
  grossProfit: number;
  totalOutstanding: number;
  totalOrders: number;
  totalCustomers: number;
  averageOrderValue: number;
}