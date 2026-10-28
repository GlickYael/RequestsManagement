import { Component, OnDestroy, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { PRIORITY_OPTIONS, RequestFilters, STATUS_OPTIONS } from '../../models/request.model';

// Presentational+interaction - holds the state of the filter fields themselves
// (including search debounce), but doesn't inject RequestsApiService and knows
// nothing about "loading from the server" - it just emits the full filter state
// upward on every change.
@Component({
  selector: 'app-request-filters-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './request-filters-bar.html',
  styleUrls: ['./request-filters-bar.css']
})
export class RequestFiltersBarComponent implements OnInit, OnDestroy {
  filtersChange = output<RequestFilters>();

  statusOptions = STATUS_OPTIONS;
  priorityOptions = PRIORITY_OPTIONS;

  sortFields: { value: string; label: string }[] = [
    { value: 'CreatedAt', label: 'תאריך יצירה' },
    { value: 'Title', label: 'כותרת' },
    { value: 'Priority', label: 'עדיפות' }
  ];

  searchQuery = signal('');
  selectedStatus = signal('');
  sortBy = signal('CreatedAt');
  sortOrder = signal('desc');
  organizationFilter = signal('');
  priorityFilter = signal('');
  assignedToFilter = signal('');
  fromDateFilter = signal('');
  toDateFilter = signal('');

  private searchSubject = new Subject<string>();
  private subscription = new Subscription();

  ngOnInit(): void {
    const sub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(query => {
      this.searchQuery.set(query);
      this.emitFilters();
    });
    this.subscription.add(sub);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  onSearchInput(event: any): void {
    this.searchSubject.next(event.target.value);
  }

  onStatusChange(value: string): void {
    this.selectedStatus.set(value);
    this.emitFilters();
  }

  onPriorityChange(value: string): void {
    this.priorityFilter.set(value);
    this.emitFilters();
  }

  // Organization/AssignedTo are free-text fields - triggered on (change) (blur/Enter)
  // rather than (input) like the search box, to avoid needing another debounce
  // mechanism for a secondary scenario
  onOrganizationChange(value: string): void {
    this.organizationFilter.set(value);
    this.emitFilters();
  }

  onAssignedToChange(value: string): void {
    this.assignedToFilter.set(value);
    this.emitFilters();
  }

  onFromDateChange(value: string): void {
    this.fromDateFilter.set(value);
    this.emitFilters();
  }

  onToDateChange(value: string): void {
    this.toDateFilter.set(value);
    this.emitFilters();
  }

  onSortByChange(value: string): void {
    this.sortBy.set(value);
    this.emitFilters();
  }

  onSortOrderChange(value: string): void {
    this.sortOrder.set(value);
    this.emitFilters();
  }

  private emitFilters(): void {
    this.filtersChange.emit({
      searchTerm: this.searchQuery(),
      status: this.selectedStatus(),
      priority: this.priorityFilter(),
      organization: this.organizationFilter(),
      assignedTo: this.assignedToFilter(),
      fromDate: this.fromDateFilter(),
      toDate: this.toDateFilter(),
      sortBy: this.sortBy(),
      sortDescending: this.sortOrder() === 'desc'
    });
  }
}
