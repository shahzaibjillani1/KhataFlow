import { ExpenseCategory } from '../../core/enums/expense-category';

export enum VoiceIntent {
  Unknown = 0,
  CreateSale = 1,
  AddUdhar = 2,
  RecordPayment = 3,
  CreateExpense = 4,
  ReportQuery = 5,
}

export interface VoiceIntentItem {
  productName: string;
  quantity: number;
}

export interface VoiceIntentResult {
  intent: VoiceIntent;
  customerName: string | null;
  paymentMethod: string | null;
  amount: number | null;
  items: VoiceIntentItem[];
  expenseCategory: string | null;
  description: string | null;
  reportQuestion: string | null;
}

export interface VoiceCommandResponse {
  intent: VoiceIntent;
  success: boolean;
  message: string | null;
  data: VoiceIntentResult | null;
  errorMessage: string | null;
}

export interface TransactionAIResponse {
  success: boolean;
  errorMessage: string | null;
  transactionType: string | null;
  amount: number | null;
  currency: string;
  person: string | null;
  category: string | null;
  date: string | null;
  description: string | null;
}

export interface ReceiptParserRequest {
  text: string;
}