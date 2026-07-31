import { CommonModule, Location } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { NotificationService } from '../../../services/notification-service';
import { LanguageService } from '../../../services/language-service';
import { LocalizedTextPipe } from '../../../shared/pipes/localized-text-pipe';
import { NotificationType } from '../../../core/models/notification-models';

type FilterKey = 'all' | 'unread' | 'payment' | 'alert' | 'info';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, TranslocoDirective, LocalizedTextPipe],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css',
})
export class Notifications implements OnInit {
  private notificationService = inject(NotificationService);
  private languageService = inject(LanguageService);
  private location = inject(Location);

  readonly lang = this.languageService.currentLang;

  notifications = this.notificationService.notifications;
  unreadCount = this.notificationService.unreadCount;

  isLoading = signal(true);
  loadError = signal<string | null>(null);
  activeFilter = signal<FilterKey>('all');

  filters: { key: FilterKey; labelKey: string }[] = [
    { key: 'all', labelKey: 'notificationsPage.filters.all' },
    { key: 'unread', labelKey: 'notificationsPage.filters.unread' },
    { key: 'payment', labelKey: 'notificationsPage.filters.payment' },
    { key: 'alert', labelKey: 'notificationsPage.filters.alert' },
    { key: 'info', labelKey: 'notificationsPage.filters.info' },
  ];

  filtered = computed(() => {
    const list = this.notifications();
    const filter = this.activeFilter();
    if (filter === 'all') return list;
    if (filter === 'unread') return list.filter((n) => !n.isRead);
    return list.filter((n) => this.categoryFor(n.type) === filter);
  });

  ngOnInit() {
    this.notificationService.fetchAll().subscribe({
      next: () => this.isLoading.set(false),
      error: (err) => {
        console.error('Failed to load notifications', err);
        this.isLoading.set(false);
        this.loadError.set('notificationsPage.loadError');
      },
    });
  }

  // Groups the backend's fine-grained NotificationType into the tab buckets.
  private categoryFor(type: NotificationType): 'payment' | 'alert' | 'info' {
    switch (type) {
      case NotificationType.PaymentReceived:
      case NotificationType.SaleRecorded:
        return 'payment';
      case NotificationType.LowStock:
      case NotificationType.OutOfStock:
      case NotificationType.ProductRestocked:
      case NotificationType.UdharReminder:
      case NotificationType.SubscriptionExpiring:
      case NotificationType.PlanDeleted:
      case NotificationType.PlanDeactivated:
      case NotificationType.PlanPriceChanged:
        return 'alert';
      default:
        return 'info';
    }
  }

  setFilter(key: FilterKey) {
    this.activeFilter.set(key);
  }

  goBack() {
    this.location.back();
  }

  markRead(id: string) {
    this.notificationService.markRead(id).subscribe({
      error: (err) => console.error('Failed to mark notification read', err),
    });
  }

  markAllRead() {
    this.notificationService.markAllRead().subscribe({
      error: (err) => console.error('Failed to mark all notifications read', err),
    });
  }

  delete(id: string, event: Event) {
    event.stopPropagation();
    this.notificationService.delete(id).subscribe({
      error: (err) => console.error('Failed to delete notification', err),
    });
  }

  timeAgo(sentAt: string): string {
    return this.notificationService.timeAgo(sentAt);
  }

  iconFor(type: NotificationType) {
    return this.notificationService.iconFor(type);
  }
}