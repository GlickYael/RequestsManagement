import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'error' | 'warning';

export interface AppNotification {
  id: number;
  type: NotificationType;
  message: string;
}

// App-wide notifications (replaces window.alert). Components push messages here;
// the single ToastComponent in AppComponent renders them.
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private nextId = 1;
  private readonly _notifications = signal<AppNotification[]>([]);

  readonly notifications = this._notifications.asReadonly();

  success(message: string): void {
    this.show('success', message);
  }

  warning(message: string): void {
    this.show('warning', message);
  }

  error(message: string): void {
    this.show('error', message);
  }

  // Notifications stay on screen until the user closes them (X)
  dismiss(id: number): void {
    this._notifications.update(list => list.filter(n => n.id !== id));
  }

  private show(type: NotificationType, message: string): void {
    const id = this.nextId++;
    this._notifications.update(list => [...list, { id, type, message }]);
  }
}
