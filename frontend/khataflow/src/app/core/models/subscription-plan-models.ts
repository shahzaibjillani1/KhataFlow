export interface SubscriptionPlan {
  id: string;
  planName: string;
  planNameUr: string | null;
  monthlyPrice: number;
  features: string[];
  featuresUr: string[];
  planType: number;
  isActive: boolean;
  userCount: number;
  totalRevenue: number;
}

export interface SubscriptionPlanAddRequest {
  planName: string;
  monthlyPrice: number;
  features: string[];
  planType: number;
}

export interface SubscriptionPlanUpdateRequest {
  id: string;
  planName: string;
  monthlyPrice: number;
  features: string[];
  isActive: boolean;
}