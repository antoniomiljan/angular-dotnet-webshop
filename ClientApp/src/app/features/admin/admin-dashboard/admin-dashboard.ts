import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardService } from '../../../core/services/dashboard.service';
import { DebugService } from '../../../core/services/debug.service';
import { Dashboard } from '../../../shared/models/dashboard.model';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
export class AdminDashboard implements OnInit {
  private dashboardService = inject(DashboardService);
  private debugService = inject(DebugService);

  // Hides the debug section from a production build. The backend also refuses the
  // request outright in Production regardless of this, so this is a UX nicety on
  // top of a real server-side guarantee, not the actual safety mechanism.
  isProduction = environment.production;

  dashboard = signal<Dashboard | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.dashboardService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load dashboard stats.');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  resetOrders(): void {
    if (!confirm('Delete all orders and order items? Products and categories are untouched. This cannot be undone.')) {
      return;
    }

    this.debugService.resetOrders().subscribe({
      next: () => this.loadDashboard(),
      error: (err) => {
        this.error.set('Failed to reset orders.');
        console.error(err);
      }
    });
  }
}
