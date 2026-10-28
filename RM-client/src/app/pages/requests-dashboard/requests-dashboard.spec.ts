import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { RequestsDashboardComponent } from './requests-dashboard';
import { RequestsPageResult } from '../../models/request.model';

function emptyPage(): RequestsPageResult {
  return { items: [], totalCount: 0, pageNumber: 1, pageSize: 10, statusCounts: {}, priorityCounts: {} };
}

describe('RequestsDashboardComponent (page/container)', () => {
  let component: RequestsDashboardComponent;
  let fixture: ComponentFixture<RequestsDashboardComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestsDashboardComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(RequestsDashboardComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should populate the requests signal after the initial load resolves', () => {
    fixture.detectChanges(); // runs ngOnInit, which triggers reload$.next() and sends the initial HTTP request

    const req = httpMock.expectOne(r => r.url.includes('/api/requests'));
    const mockResponse: RequestsPageResult = {
      ...emptyPage(),
      items: [
        {
          id: 1,
          title: 'Test Request',
          organizationName: 'Org A',
          status: 'New',
          priority: 'Low',
          assignedTo: 'Dev',
          createdAt: new Date().toISOString(),
          updatedAt: null,
          version: 1
        }
      ],
      totalCount: 1
    };
    req.flush(mockResponse);

    // Indirect test of the Zoneless/Signals fix: state is updated from an asynchronous
    // callback, and here we check that it was indeed reflected in the signal.
    expect(component.requests().length).toBe(1);
    expect(component.totalCount()).toBe(1);
    expect(component.isLoading()).toBe(false);
  });

  it('should reload with the new filters when onFiltersChange is called', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url.includes('/api/requests')).flush(emptyPage()); // initial load

    component.onFiltersChange({
      searchTerm: '',
      status: 'InProgress',
      priority: '',
      organization: '',
      assignedTo: '',
      fromDate: '',
      toDate: '',
      sortBy: 'CreatedAt',
      sortDescending: true
    });

    const req = httpMock.expectOne(r => r.url.includes('/api/requests'));
    expect(req.request.params.get('Status')).toBe('InProgress');
    expect(req.request.params.get('PageNumber')).toBe('1'); // a filter change resets the page
    req.flush(emptyPage());
  });

  it('should open and close the audit history modal by request id', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url.includes('/api/requests')).flush(emptyPage());

    expect(component.selectedAuditRequestId()).toBeNull();

    component.onOpenHistory(42);
    expect(component.selectedAuditRequestId()).toBe(42);

    component.onCloseHistory();
    expect(component.selectedAuditRequestId()).toBeNull();
  });
});
