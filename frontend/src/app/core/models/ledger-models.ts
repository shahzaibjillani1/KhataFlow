export interface LedgerLineItem {
  productName: string;
  productNameUr: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface LedgerEntry {
  id: string;
  type: 'Udhar' | 'Cash';
  amount: number;
  notes: string;
  runningBalance: number;
  createdAt: string;
  items: LedgerLineItem[] | null;
}

export interface CustomerKhata {
  customerId: string;
  customerName: string;
  customerNameUr: string | null;
  phoneNumber: string;
  totalPurchases: number;
  totalPaid: number;
  outstanding: number;
  entries: LedgerEntry[];
}

export interface AddUdharRequest {
  customerId: string;
  amount: number;
  notes: string;
}

export interface RecordPaymentRequest {
  customerId: string;
  amount: number;
  notes: string;
}