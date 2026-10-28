import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PaginationComponent } from './pagination';

describe('PaginationComponent', () => {
  let component: PaginationComponent;
  let fixture: ComponentFixture<PaginationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaginationComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(PaginationComponent);
    component = fixture.componentInstance;
  });

  it('should emit pageChange with the next page number when hasNextPage is true', () => {
    fixture.componentRef.setInput('page', 2);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.detectChanges();

    const emissions: number[] = [];
    component.pageChange.subscribe(p => emissions.push(p));

    component.goToNext();
    component.goToPrevious();

    expect(emissions).toEqual([3, 1]);
  });

  it('should not emit pageChange past the last page', () => {
    fixture.componentRef.setInput('page', 5);
    fixture.componentRef.setInput('hasNextPage', false);
    fixture.detectChanges();

    const emissions: number[] = [];
    component.pageChange.subscribe(p => emissions.push(p));

    component.goToNext();

    expect(emissions.length).toBe(0);
  });
});
