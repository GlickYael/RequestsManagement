import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AggregationsPanelComponent } from './aggregations-panel';

describe('AggregationsPanelComponent', () => {
  let fixture: ComponentFixture<AggregationsPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AggregationsPanelComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(AggregationsPanelComponent);
    fixture.componentRef.setInput('totalCount', 42);
    fixture.componentRef.setInput('statusCounts', { New: 10, Completed: 32 });
    fixture.componentRef.setInput('priorityCounts', { Low: 20, High: 22 });
    fixture.detectChanges();
  });

  it('should create and render the total count from its input', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('42');
    expect(text).toContain('New');
    expect(text).toContain('10');
  });
});
