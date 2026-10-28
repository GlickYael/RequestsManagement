import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RequestsApiService } from '../../services/requests';
import { NotificationService } from '../../services/notification';
import { RequestStatusAuditDto } from '../../models/request.model';

// Independent smart leaf component: receives only a requestId, and injects
// RequestsApiService itself to fetch and manage its own audit history - the parent
// doesn't need to know anything about audit.
@Component({
  selector: 'app-audit-history-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './audit-history-modal.html',
  styleUrls: ['./audit-history-modal.css']
})
export class AuditHistoryModalComponent implements OnInit {
  private requestsApi = inject(RequestsApiService);
  private notifications = inject(NotificationService);

  requestId = input.required<number>();
  close = output<void>();

  auditLoading = signal(false);
  auditHistory = signal<RequestStatusAuditDto[]>([]);

  ngOnInit(): void {
    this.auditLoading.set(true);
    this.requestsApi.getRequestAudit(this.requestId()).subscribe({
      next: (data) => {
        this.auditHistory.set(data);
        this.auditLoading.set(false);
      },
      error: () => {
        this.notifications.error('שגיאה בטעינת היסטוריית השינויים.');
        this.auditLoading.set(false);
      }
    });
  }
}
