import { ExpenseCategory } from "../enums/expense-category";

export interface Expense {
  id: string;
  title: string;
  titleUr: string | null;
  amount: number;
  category: ExpenseCategory;
  note: string | null;
  noteUr: string | null;
  date: string;
}

export interface ExpenseAddRequest {
  title: string;
  amount: number;
  category: ExpenseCategory;
  note?: string | null;
  date?: string | null;
}

export interface ExpenseByCategory {
  category: ExpenseCategory;
  total: number;
}