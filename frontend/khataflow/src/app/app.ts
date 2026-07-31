import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationHub } from './services/notification-hub';
import { NotificationService } from './services/notification-service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  protected readonly title = signal('khataflow');
  private hub = inject(NotificationHub);
  private notifService = inject(NotificationService);

  async ngOnInit(): Promise<void> {
    const token = localStorage.getItem('accessToken');
    if (!token) return;

    if (this.isTokenExpired(token)) {
      localStorage.removeItem('accessToken');
      return;
    }

    await this.hub.startConnection(token);
    this.notifService.syncUnreadCount();
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 < Date.now();
    } catch {
      return true;
    }
  }
}