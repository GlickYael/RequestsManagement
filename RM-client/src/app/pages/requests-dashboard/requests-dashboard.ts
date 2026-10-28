import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { RequestsApiService, RequestsQueryParams } from '../../services/requests';
import { DEFAULT_FILTERS, RequestFilters, RequestItem } from '../../models/request.model';
import { AggregationsPanelComponent } from '../../components/aggregations-panel/aggregations-panel';
import { RequestFiltersBarComponent } from '../../components/request-filters-bar/request-filters-bar';
import { RequestsTableComponent } from '../../components/requests-table/requests-table';
import { PaginationComponent } from '../../components/pagination/pagination';
import { AuditHistoryModalComponent } from '../../components/audit-history-modal/audit-history-modal';

// Smart/Container: the only component in this tree that injects RequestsApiService to
// fetch the list itself, and holds the shared state (current filters, page, results).
// Every other component (filters-bar/table/pagination/aggregations/audit-modal) is either
// Presentational or an independent Smart-leaf that only communicates with it via Input/Output.
@Component({
  selector: 'app-requests-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    AggregationsPanelComponent,
    RequestFiltersBarComponent,
    RequestsTableComponent,
    PaginationComponent,
    AuditHistoryModalComponent
  ],
  templateUrl: './requests-dashboard.html',
  styleUrls: ['./requests-dashboard.css']
})
export class RequestsDashboardComponent implements OnInit, OnDestroy {
  // This app is Zoneless (no zone.js in the project) - any state updated from an
  // asynchronous callback (HTTP subscribe) must be a Signal.
  requests = signal<RequestItem[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  statusCounts = signal<Record<string, number>>({});
  priorityCounts = signal<Record<string, number>>({});

  currentFilters = signal<RequestFilters>(DEFAULT_FILTERS);
  page = signal(1);
  pageSize = 10;
  totalCount = signal(0);

  hasNextPage = computed(() => this.page() * this.pageSize < this.totalCount());

  selectedAuditRequestId = signal<number | null>(null);

  // Single trigger for every reload (filters/page) - goes through one switchMap so a new
  // request always cancels/replaces a previous one still in flight (prevents race conditions).
  private reload$ = new Subject<void>();
  private subscription = new Subscription();

  constructor(private requestsApi: RequestsApiService) {}

  ngOnInit(): void {
    const sub = this.reload$.pipe(
      switchMap(() => {
        this.isLoading.set(true);
        this.errorMessage.set(null);
        return this.requestsApi.getRequests(this.buildQueryParams());
      })
    ).subscribe({
      next: (response) => {
        this.requests.set(response.items);
        this.totalCount.set(response.totalCount);
        this.statusCounts.set(response.statusCounts);
        this.priorityCounts.set(response.priorityCounts);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('שגיאה בטעינת הנתונים מהשרת.');
        this.isLoading.set(false);
      }
    });
    this.subscription.add(sub);

    this.reload$.next();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  onFiltersChange(filters: RequestFilters): void {
    this.currentFilters.set(filters);
    this.page.set(1);
    this.reload$.next();
  }

  changePage(newPage: number): void {
    if (newPage < 1) return;
    this.page.set(newPage);
    this.reload$.next();
  }

  onRefreshNeeded(): void {
    this.reload$.next();
  }

  onOpenHistory(requestId: number): void {
    this.selectedAuditRequestId.set(requestId);
  }

  onCloseHistory(): void {
    this.selectedAuditRequestId.set(null);
  }

  private buildQueryParams(): RequestsQueryParams {
    const filters = this.currentFilters();
    return {
      pageNumber: this.page(),
      pageSize: this.pageSize,
      searchTerm: filters.searchTerm,
      status: filters.status,
      priority: filters.priority,
      organization: filters.organization,
      assignedTo: filters.assignedTo,
      fromDate: filters.fromDate,
      toDate: filters.toDate,
      sortBy: filters.sortBy,
      sortDescending: filters.sortDescending
    };
  }
}
