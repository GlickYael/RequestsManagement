import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RequestsDashboardComponent } from './pages/requests-dashboard/requests-dashboard';
import { ToastComponent } from './components/toast/toast';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RequestsDashboardComponent, ToastComponent],
  template: `
    <header class="app-header">
      <h1>מערכת ניהול פניות - רשות (Requests Management)</h1>
    </header>

    <main>
      <app-requests-dashboard></app-requests-dashboard>
    </main>

    <app-toast></app-toast>
  `,
  styleUrls: ['./app.css']
})
export class AppComponent {}
