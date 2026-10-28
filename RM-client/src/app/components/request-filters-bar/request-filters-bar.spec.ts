import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RequestFiltersBarComponent } from './request-filters-bar';
import { RequestFilters } from '../../models/request.model';

describe('RequestFiltersBarComponent', () => {
  let component: RequestFiltersBarComponent;
  let fixture: ComponentFixture<RequestFiltersBarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestFiltersBarComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(RequestFiltersBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // runs ngOnInit, which sets up the debounce subscription on the search box
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should debounce rapid search input into exactly one filtersChange emission', async () => {
    const emissions: RequestFilters[] = [];
    component.filtersChange.subscribe(f => emissions.push(f));

    component.onSearchInput({ target: { value: 'a' } });
    component.onSearchInput({ target: { value: 'ab' } });
    component.onSearchInput({ target: { value: 'abc' } });

    // The debounce window is 300ms - we wait past that to verify only the last value is emitted, exactly once
    await new Promise(resolve => setTimeout(resolve, 400));

    expect(emissions.length).toBe(1);
    expect(emissions[0].searchTerm).toBe('abc');
  });

  it('should emit the full filters object immediately when the status filter changes', () => {
    const emissions: RequestFilters[] = [];
    component.filtersChange.subscribe(f => emissions.push(f));

    component.onStatusChange('Waiting');

    expect(emissions.length).toBe(1);
    expect(emissions[0].status).toBe('Waiting');
    expect(emissions[0].sortBy).toBe('CreatedAt'); // the rest of the fields are sent at their default values
  });
});
