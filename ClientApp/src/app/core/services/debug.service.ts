import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DebugService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/debug`;

  // Backend also 404s this outright in Production regardless of caller, so this
  // service being reachable in a prod build still can't do anything there.
  resetOrders(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/reset-orders`);
  }
}
