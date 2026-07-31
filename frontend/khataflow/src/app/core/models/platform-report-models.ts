export interface GrowthReportResponse {
  labels: string[];
  users: number[];
  businesses: number[];
  revenue: number[];
}

export interface PlanRevenueBreakdownResponse {
  planId: string;
  planName: string;
  revenue: number;
  percentageOfTotal: number;
}

export interface TopBusinessResponse {
  businessId: string;
  businessName: string;
  revenue: number;
  planName: string;
  percentageOfTop: number;
}

export interface RecentActivityResponse {
  id: string;
  message: string;
  type: string;
  timestamp: string;
}

export interface PlatformReportResponse {
  growth: GrowthReportResponse;
  revenueByPlan: PlanRevenueBreakdownResponse[];
  topBusinesses: TopBusinessResponse[];
  recentActivity: RecentActivityResponse[];
}