interface NotificationItem {
  id: number;
  heading: string;
  desc: string;
  time: string;
  iconClass: string;
  iconBgClass: string;
  read: boolean;
  type: 'payment' | 'alert' | 'overdue' | 'info';
}
