import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Dashboard } from '../../shared/models/dashboard.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/dashboard`;

  getDashboard(): Observable<Dashboard> {
    return this.http.get<Dashboard>(this.baseUrl);
  }
}
