import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { AppNotification } from '../core/models/notification-models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class NotificationHub implements OnDestroy {
  private readonly HUB_URL = environment.hubUrl;

  private connection: signalR.HubConnection | null = null;

  private notificationSubject = new BehaviorSubject<AppNotification | null>(null);
  private connectionStateSubject = new BehaviorSubject<boolean>(false);

  /** Emits null once at startup (no notification yet), then each incoming AppNotification. */
  notification$: Observable<AppNotification | null> = this.notificationSubject.asObservable();

  isConnected$: Observable<boolean> = this.connectionStateSubject.asObservable();

  async startConnection(token: string): Promise<void> {
    if (
      this.connection?.state === signalR.HubConnectionState.Connected ||
      this.connection?.state === signalR.HubConnectionState.Connecting
    )
      return;

    if (this.connection) {
      await this.connection.stop().catch(() => {});
      this.connection = null;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.HUB_URL, { accessTokenFactory: () => token })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerHandlers();

    this.connection.onreconnecting(() => this.connectionStateSubject.next(false));
    this.connection.onreconnected(() => this.connectionStateSubject.next(true));
    this.connection.onclose(() => this.connectionStateSubject.next(false));

    try {
      await this.connection.start();
      this.connectionStateSubject.next(true);
    } catch (err) {
      console.error('SignalR connection failed:', err);
      this.connectionStateSubject.next(false);
    }
  }

  private registerHandlers(): void {
    if (!this.connection) return;

    this.connection.on('ReceiveNotification', (notification: AppNotification) => {
      this.notificationSubject.next(notification);
    });
  }

  async stopConnection(): Promise<void> {
    if (!this.connection) return;
    await this.connection.stop();
    this.connection = null;
    this.connectionStateSubject.next(false);
  }

  ngOnDestroy(): void {
    this.stopConnection();
  }
}