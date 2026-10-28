import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

// Presentational only - doesn't inject any Service, just displays what it receives via Input.
@Component({
  selector: 'app-aggregations-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './aggregations-panel.html',
  styleUrls: ['./aggregations-panel.css']
})
export class AggregationsPanelComponent {
  totalCount = input.required<number>();
  statusCounts = input.required<Record<string, number>>();
  priorityCounts = input.required<Record<string, number>>();
}
