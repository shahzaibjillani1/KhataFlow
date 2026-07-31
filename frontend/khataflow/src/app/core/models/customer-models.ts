export interface Customer {
  id: string;
  name: string;
  nameUr: string | null;
  phoneNumber: string;
  address: string;
  addressUr: string | null;
  lastVisit: string;
  totalPurchases: number;
  udharAmount: number;
  publicToken: string;
}

export interface PaginatedCustomerResponse {
  items: Customer[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalOutstanding: number;
}

export interface CustomerAddRequest {
  name: string;
  phoneNumber: string;
  address: string;
  businessId: string;
  lastVisit: string;
  totalPurchases: number;
  udharAmount: number;
}

export interface CustomerUpdateRequest extends CustomerAddRequest {
  id: string;
}