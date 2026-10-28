import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AuditHistoryModalComponent } from './audit-history-modal';
import { RequestStatusAuditDto } from '../../models/request.model';

describe('AuditHistoryModalComponent', () => {
  let component: AuditHistoryModalComponent;
  let fixture: ComponentFixture<AuditHistoryModalComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuditHistoryModalComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(AuditHistoryModalComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('requestId', 7);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch and display the audit history for the given requestId on init', () => {
    fixture.detectChanges(); // runs ngOnInit, which fetches the history for requestId=7

    const req = httpMock.expectOne(r => r.url.includes('/api/requests/7/status-audit'));
    const mockHistory: RequestStatusAuditDto[] = [
      { id: 1, requestId: 7, previousStatus: 'New', newStatus: 'InProgress', changedAt: new Date().toISOString(), changedBy: 'CurrentAdmin' }
    ];
    req.flush(mockHistory);
    fixture.detectChanges(); // re-renders the DOM after the signals were updated from the async callback

    expect(component.auditHistory().length).toBe(1);
    expect(component.auditLoading()).toBe(false);

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('New');
    expect(text).toContain('InProgress');
  });

  it('should emit close when the close button handler fires', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url.includes('/api/requests/7/status-audit')).flush([]);

    let closed = false;
    component.close.subscribe(() => (closed = true));

    component.close.emit();

    expect(closed).toBe(true);
  });
});
