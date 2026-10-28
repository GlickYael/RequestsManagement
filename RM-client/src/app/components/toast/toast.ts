import { Component, inject } from '@angular/core';
import { NotificationService } from '../../services/notification';

@Component({
  selector: 'app-toast',
  standalone: true,
  template: `
    <div class="toast-container" aria-live="polite">
      @for (n of notifications(); track n.id) {
        <div class="toast" [class]="'toast-' + n.type" role="status">
          <span class="toast-message">{{ n.message }}</span>
          <button type="button" class="toast-close" aria-label="סגירה" (click)="dismiss(n.id)">×</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 16px;
      left: 16px;
      z-index: 1000;
      display: flex;
      flex-direction: column;
      gap: 8px;
      max-width: min(420px, calc(100vw - 32px));
    }
    .toast {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px 14px;
      border-radius: 6px;
      color: #fff;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
      direction: rtl;
    }
    .toast-message { flex: 1; white-space: pre-line; }
    .toast-success { background: #28a745; }
    .toast-warning { background: #e0a800; color: #212529; }
    .toast-error { background: #dc3545; }
    .toast-close {
      background: none;
      border: none;
      color: inherit;
      font-size: 18px;
      line-height: 1;
      cursor: pointer;
    }
  `]
})
export class ToastComponent {
  private notificationService = inject(NotificationService);

  notifications = this.notificationService.notifications;

  dismiss(id: number): void {
    this.notificationService.dismiss(id);
  }
}
