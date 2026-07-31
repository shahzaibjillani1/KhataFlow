export interface CustomerLedgerViewLineItem {
  productName: string;
  productNameUr: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface CustomerLedgerViewEntry {
  type: 'Udhar' | 'Cash';
  amount: number;
  date: string;
  description: string;
  runningBalance: number;
  items: CustomerLedgerViewLineItem[] | null;
}

export interface CustomerLedgerViewResponse {
  customerName: string;
  customerNameUr: string | null;
  businessName: string;
  businessNameUr: string | null;
  currentBalance: number;
  currency: string;
  history: CustomerLedgerViewEntry[];
}