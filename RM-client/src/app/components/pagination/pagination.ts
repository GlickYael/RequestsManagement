import { Component, input, output } from '@angular/core';

// Fully generic - doesn't know about "requests" at all, just a page number and
// whether there's a next page. Reusable for any other paginated list in the app.
@Component({
  selector: 'app-pagination',
  standalone: true,
  templateUrl: './pagination.html',
  styleUrls: ['./pagination.css']
})
export class PaginationComponent {
  page = input.required<number>();
  hasNextPage = input.required<boolean>();
  pageChange = output<number>();

  goToPrevious(): void {
    if (this.page() > 1) {
      this.pageChange.emit(this.page() - 1);
    }
  }

  goToNext(): void {
    if (this.hasNextPage()) {
      this.pageChange.emit(this.page() + 1);
    }
  }
}
