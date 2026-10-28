import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { RequestsTableComponent } from './requests-table';
import { RequestItem } from '../../models/request.model';
import { NotificationService } from '../../services/notification';

function sampleRequest(overrides: Partial<RequestItem> = {}): RequestItem {
  return {
    id: 1,
    title: 'Sample',
    organizationName: 'Org A',
    status: 'New',
    priority: 'Low',
    assignedTo: 'Dev',
    createdAt: new Date().toISOString(),
    updatedAt: null,
    version: 1,
    ...overrides
  };
}

describe('RequestsTableComponent', () => {
  let component: RequestsTableComponent;
  let fixture: ComponentFixture<RequestsTableComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestsTableComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(RequestsTableComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('requests', [sampleRequest()]);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create and render the given requests', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Sample');
  });

  it('should track row selection via toggleSelection', () => {
    expect(component.selectedRequestIds().has(1)).toBe(false);

    component.toggleSelection(1, { target: { checked: true } });
    expect(component.selectedRequestIds().has(1)).toBe(true);

    component.toggleSelection(1, { target: { checked: false } });
    expect(component.selectedRequestIds().has(1)).toBe(false);
  });

  it('should emit refreshNeeded after a successful row status update', () => {
    let refreshed = false;
    component.refreshNeeded.subscribe(() => (refreshed = true));

    component.updateRequestStatus(1, 'InProgress', 1);

    const req = httpMock.expectOne(r => r.url.includes('/api/requests/1/status'));
    expect(req.request.method).toBe('PATCH');
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(refreshed).toBe(true);
  });

  it('should show a conflict warning and refresh on 409', () => {
    const notifications = TestBed.inject(NotificationService);
    let refreshed = false;
    component.refreshNeeded.subscribe(() => (refreshed = true));

    component.updateRequestStatus(1, 'InProgress', 1);

    const req = httpMock.expectOne(r => r.url.includes('/api/requests/1/status'));
    req.flush(null, { status: 409, statusText: 'Conflict' });

    const shown = notifications.notifications();
    expect(shown.length).toBe(1);
    expect(shown[0].type).toBe('warning');
    expect(shown[0].message).toContain('409');
    expect(refreshed).toBe(true);
  });
});
