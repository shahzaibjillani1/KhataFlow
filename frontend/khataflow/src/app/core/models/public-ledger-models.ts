export interface PublicLedgerLineItem {
  productName: string;
  productNameUr: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface PublicLedgerEntry {
  type: 'Udhar' | 'Cash';
  amount: number;
  date: string;
  description: string;
  runningBalance: number;
  items: PublicLedgerLineItem[] | null;
}

export interface PublicLedgerData {
  customerName: string;
  customerNameUr: string | null;
  businessName: string;
  businessNameUr: string | null;
  currentBalance: number;
  currency: string;
  history: PublicLedgerEntry[];
}