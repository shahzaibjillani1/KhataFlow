import { PaymentStatus } from "../enums/payment-status";

export interface Sale {
  id: string;
  invoiceNumber: string;
  date: string;
  customerName: string;
  customerNameUr: string | null;
  totalAmount: number;
  itemCount: number;
  paymentStatus: PaymentStatus;
  items?: SaleItemDetail[];
}

export interface SaleItemRequest {
  productId: string;
  quantity: number;
}
export interface SaleItemDetail {
  productId: string;
  productName: string;
  productNameUr: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}
export interface SaleAddRequest {
  customerId: string | null;
  paymentStatus: number;
  note: string;
  items: SaleItemRequest[];
}

export interface WeeklySales {
  day: string;
  totalSales: number;
}

export interface MonthlyRevenue {
  month: string;
  totalRevenue: number;
}