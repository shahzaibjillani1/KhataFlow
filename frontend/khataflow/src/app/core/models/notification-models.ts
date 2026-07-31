export enum NotificationType {
  System = 0,
  LowStock = 1,
  OutOfStock = 2,
  ProductRestocked = 3,
  UdharReminder = 4,
  PaymentReceived = 5,
  SaleRecorded = 6,
  SubscriptionExpiring = 7,
  DailySummary = 8,
  PlanDeleted = 9,
  PlanReactivated = 10,
  PlanDeactivated = 11,
  PlanPriceChanged = 12,
  PlanCreated = 13,
  NewBusinessRegistered = 14,
  WelcomeMessage = 15,
}

export interface AppNotification {
  id: string;
  title: string;
  titleUr: string | null;
  message: string;
  messageUr: string | null;
  type: NotificationType;
  isRead: boolean;
  sentAt: string;
  referenceId: string | null;
}