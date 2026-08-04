import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

interface CreateIntentResponse {
  clientSecret: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/payments`;

  createPaymentIntent(orderId: number, token: string): Observable<CreateIntentResponse> {
    return this.http.post<CreateIntentResponse>(`${this.baseUrl}/create-intent/${orderId}`, {}, { params: { token } });
  }
}