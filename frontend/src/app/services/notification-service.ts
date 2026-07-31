// core/services/notification-service.ts (formerly notification.ts / notification-service.ts)
import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import { environment } from '../../environments/environment';
import { AppNotification, NotificationType } from '../core/models/notification-models';
import { ApiResponse } from '../core/models/auth-models';
import { NotificationHub } from './notification-hub';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private translocoService = inject(TranslocoService);
  private hub = inject(NotificationHub);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/Notification`;

  private readonly _notifications = signal<AppNotification[]>([]);
  readonly notifications = this._notifications.asReadonly();
  readonly unreadCount = computed(
    () => this._notifications().filter((n) => !n.isRead).length
  );

  constructor() {
    // Single subscription for the lifetime of the app (this service is a root singleton,
    // so this never needs manual teardown). Folds live pushes into the same signal REST
    // fetches populate, so unreadCount is always derived from one list.
    this.hub.notification$.subscribe((notification) => {
      if (!notification) return;
      this._notifications.update((list) => [notification, ...list]);
    });
  }

  /** Call once after the SignalR connection is established (or on app bootstrap) to hydrate state. */
  syncUnreadCount(): void {
    this.fetchAll().subscribe({
      error: (err) => console.error('Failed to sync notifications:', err),
    });
  }

  fetchAll() {
    return this.http
      .get<ApiResponse<AppNotification[]>>(this.baseUrl)
      .pipe(tap((res) => this._notifications.set(res.data)));
  }

  fetchUnread() {
    return this.http.get<ApiResponse<AppNotification[]>>(`${this.baseUrl}/unread`);
  }

  fetchUnreadCount() {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/unread-count`);
  }

  getById(id: string) {
    return this.http.get<ApiResponse<AppNotification>>(`${this.baseUrl}/${id}`);
  }

  markRead(id: string) {
    return this.http.patch<ApiResponse<AppNotification>>(`${this.baseUrl}/${id}/read`, {}).pipe(
      tap(() => {
        this._notifications.update((list) =>
          list.map((n) => (n.id === id ? { ...n, isRead: true } : n))
        );
      })
    );
  }

  markAllRead() {
    return this.http.patch<ApiResponse<null>>(`${this.baseUrl}/mark-all-read`, {}).pipe(
      tap(() => {
        this._notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
      })
    );
  }

  delete(id: string) {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`).pipe(
      tap(() => {
        this._notifications.update((list) => list.filter((n) => n.id !== id));
      })
    );
  }

  timeAgo(sentAt: string): string {
    const diffMs = Date.now() - new Date(sentAt).getTime();
    const minutes = Math.floor(diffMs / 60000);

    if (minutes < 1) return this.translocoService.translate('header.timeAgo.justNow');

    if (minutes < 60) {
      const key = minutes === 1 ? 'header.timeAgo.minuteAgo' : 'header.timeAgo.minutesAgo';
      return this.translocoService.translate(key, { count: minutes });
    }

    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
      const key = hours === 1 ? 'header.timeAgo.hourAgo' : 'header.timeAgo.hoursAgo';
      return this.translocoService.translate(key, { count: hours });
    }

    const days = Math.floor(hours / 24);
    if (days === 1) return this.translocoService.translate('header.timeAgo.yesterday');
    return this.translocoService.translate('header.timeAgo.daysAgo', { count: days });
  }

  iconFor(type: NotificationType): { icon: string; iconBg: string } {
    switch (type) {
      case NotificationType.PaymentReceived:
        return { icon: 'fa-solid fa-credit-card', iconBg: 'bg-green-100 text-green-600' };
      case NotificationType.LowStock:
      case NotificationType.UdharReminder:
      case NotificationType.SubscriptionExpiring:
        return { icon: 'fa-solid fa-triangle-exclamation', iconBg: 'bg-amber-100 text-amber-600' };
      case NotificationType.OutOfStock:
        return { icon: 'fa-solid fa-circle-exclamation', iconBg: 'bg-red-100 text-red-500' };
      case NotificationType.ProductRestocked:
        return { icon: 'fa-solid fa-box', iconBg: 'bg-green-100 text-green-600' };
      case NotificationType.SaleRecorded:
        return { icon: 'fa-solid fa-cash-register', iconBg: 'bg-green-100 text-green-600' };
      case NotificationType.WelcomeMessage:
      case NotificationType.NewBusinessRegistered:
      case NotificationType.PlanCreated:
        return { icon: 'fa-solid fa-circle-info', iconBg: 'bg-indigo-100 text-indigo-500' };
      case NotificationType.PlanDeleted:
      case NotificationType.PlanDeactivated:
        return { icon: 'fa-solid fa-ban', iconBg: 'bg-red-100 text-red-500' };
      case NotificationType.DailySummary:
        return { icon: 'fa-solid fa-chart-line', iconBg: 'bg-indigo-100 text-indigo-500' };
      default:
        return { icon: 'fa-solid fa-bell', iconBg: 'bg-slate-100 text-slate-500' };
    }
  }
}