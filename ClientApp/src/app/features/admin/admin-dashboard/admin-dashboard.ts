import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink, MatCardModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
// Placeholder: links out to the other admin pages, no data or stats of its own yet.
export class AdminDashboard {}