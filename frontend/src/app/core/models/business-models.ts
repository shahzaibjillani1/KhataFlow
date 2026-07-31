export interface Business {
  id: string;
  name: string;
  nameUr: string | null,
  email: string;
  phoneNumber: string;
  address: string | null;
  addressUr: string | null;
  status: number;
  plan: number;
  subscriptionExpiry: string;
  registeredAt: string;
}

export interface BusinessAddRequest {
  name: string;
  ownerEmail: string;
  ownerName: string;
  phoneNumber: string;
  address: string;
  plan: number;
}

export interface BusinessUpdateRequest {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  address: string;
}

export interface PlatformSummary {
  totalUsers: number;
  activeSubscriptions: number;
  newThisWeek: number;
  platformRevenue: number;
  totalUserSales: number;
  churnRate: number;
  arpu: number;
}

export interface ChangeSubscriptionRequest {
  newPlan: number;
  customExpiryDate: string;
}
