import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from './auth.service';

export interface NotificationItem {
  id: string;
  title: string;
  message: string;
  notificationClass?: string;
  isRead: boolean;
  createdAtUtc: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRNotificationService {
  private hubConnection: signalR.HubConnection | null = null;
  notifications = signal<NotificationItem[]>([]);
  unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);
  connectionState = signal<'Connected' | 'Connecting' | 'Disconnected'>('Disconnected');

  constructor(
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private http: HttpClient
  ) {}

  startConnection(): void {
    const token = this.authService.getAccessToken();
    if (!token) return;

    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connectionState.set('Connecting');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => this.authService.getAccessToken() || ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hubConnection.on('ReceiveNotification', (payload: any) => {
      this.handleIncomingNotification({
        id: payload.id || crypto.randomUUID(),
        title: payload.title || 'Notification',
        message: payload.message || JSON.stringify(payload),
        notificationClass: payload.notificationClass || 'General',
        isRead: false,
        createdAtUtc: payload.createdAtUtc || new Date().toISOString()
      });
    });

    this.hubConnection.on('ReceiveEvent', (event: any) => {
      this.handleIncomingNotification({
        id: crypto.randomUUID(),
        title: `Event: ${event.eventType || 'System Update'}`,
        message: typeof event.payload === 'string' ? event.payload : JSON.stringify(event.payload),
        notificationClass: 'Event',
        isRead: false,
        createdAtUtc: event.timestampUtc || new Date().toISOString()
      });
    });

    this.hubConnection.start()
      .then(() => {
        this.connectionState.set('Connected');
        this.fetchRecentNotifications();
      })
      .catch(() => {
        this.connectionState.set('Disconnected');
      });

    this.hubConnection.onclose(() => this.connectionState.set('Disconnected'));
    this.hubConnection.onreconnecting(() => this.connectionState.set('Connecting'));
    this.hubConnection.onreconnected(() => this.connectionState.set('Connected'));
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.connectionState.set('Disconnected');
    }
  }

  fetchRecentNotifications(): void {
    this.http.get<{ items: any[] }>('/api/v1/notifications?pageSize=20').subscribe({
      next: res => {
        if (res && res.items) {
          const list: NotificationItem[] = res.items.map(item => ({
            id: item.id,
            title: item.title,
            message: item.message,
            notificationClass: item.notificationClass,
            isRead: item.isRead,
            createdAtUtc: item.createdAtUtc
          }));
          this.notifications.set(list);
        }
      },
      error: () => {}
    });
  }

  markAsRead(id: string): void {
    this.notifications.update(list =>
      list.map(n => (n.id === id ? { ...n, isRead: true } : n))
    );
    this.http.post(`/api/v1/notifications/${id}/read`, {}).subscribe({
      error: () => {}
    });
  }

  markAllAsRead(): void {
    this.notifications.update(list =>
      list.map(n => ({ ...n, isRead: true }))
    );
    this.http.post('/api/v1/notifications/read-all', {}).subscribe({
      error: () => {}
    });
  }

  private handleIncomingNotification(item: NotificationItem): void {
    this.notifications.update(existing => [item, ...existing]);

    this.snackBar.open(`${item.title}: ${item.message}`, 'Close', {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'bottom',
      panelClass: ['snack-primary']
    });
  }
}
