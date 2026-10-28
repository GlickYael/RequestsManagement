import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RequestsApiService } from '../../services/requests';
import { NotificationService } from '../../services/notification';
import { RequestItem, STATUS_OPTIONS } from '../../models/request.model';

// Also contains row selection and the Bulk action (tightly coupled to row selection).
// "Half-smart": injects RequestsApiService and performs the status updates itself
// (row/Bulk), but isn't responsible for deciding when to reload the list - it just
// emits refreshNeeded to the parent.
@Component({
  selector: 'app-requests-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './requests-table.html',
  styleUrls: ['./requests-table.css']
})
export class RequestsTableComponent {
  private requestsApi = inject(RequestsApiService);
  private notifications = inject(NotificationService);

  requests = input.required<RequestItem[]>();
  openHistory = output<number>();
  refreshNeeded = output<void>();

  statusOptions = STATUS_OPTIONS;

  selectedRequestIds = signal<Set<number>>(new Set<number>());
  bulkTargetStatus = signal<string>(STATUS_OPTIONS[1]);

  toggleSelection(id: number, event: any): void {
    const checked = event.target.checked;
    this.selectedRequestIds.update(ids => {
      const next = new Set(ids);
      if (checked) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }

  updateRequestStatus(id: number, newStatus: string, currentVersion: number): void {
    this.requestsApi.updateStatus(id, { status: newStatus, version: currentVersion }).subscribe({
      next: () => {
        this.notifications.success('הסטטוס עודכן בהצלחה!');
        this.refreshNeeded.emit();
      },
      error: (err) => {
        if (err.status === 409) {
          this.notifications.warning('שגיאת התנגשות (409 Conflict): הפנייה עודכנה על ידי משתמש אחר. הנתונים יתרעננו כעת.');
          this.refreshNeeded.emit();
        } else {
          this.notifications.error('שגיאה בעדכון הסטטוס. נסה שוב.');
        }
      }
    });
  }

  executeBulkUpdate(): void {
    if (this.selectedRequestIds().size === 0) {
      this.notifications.warning('אנא בחר לפחות פנייה אחת לעדכון.');
      return;
    }

    const selectedIds = this.selectedRequestIds();
    const itemsToUpdate = this.requests()
      .filter(req => selectedIds.has(req.id))
      .map(req => ({
        id: req.id,
        version: req.version,
        oldStatus: req.status
      }));

    const bulkDto = {
      status: this.bulkTargetStatus(),
      items: itemsToUpdate
    };

    this.requestsApi.bulkUpdateStatus(bulkDto).subscribe({
      next: (failures) => {
        if (failures && failures.length > 0) {
          let msg = 'העדכון הסתיים עם שגיאות בחלק מהפניות:\n';
          failures.forEach(f => {
            msg += `- פנייה מספר ${f.id}: ${f.message}\n`;
          });
          this.notifications.warning(msg);
        } else {
          this.notifications.success('כל הפניות עודכנו בהצלחה!');
        }

        this.selectedRequestIds.set(new Set());
        this.refreshNeeded.emit();
      },
      error: () => {
        this.notifications.error('שגיאה בביצוע העדכון הקבוצתי.');
      }
    });
  }
}
