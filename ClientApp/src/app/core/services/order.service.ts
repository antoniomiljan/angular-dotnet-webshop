import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateOrderRequest, Order } from '../../shared/models/order.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/orders`;

  createOrder(request: CreateOrderRequest): Observable<Order> {
    return this.http.post<Order>(this.baseUrl, request);
  }

  getOrder(id: number, token: string): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/${id}`, { params: { token } });
  }

  getAllOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(this.baseUrl);
  }

  updateOrderStatus(id: number, status: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/status`, { status });
  }

  cancelOrder(id: number, token: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, {}, { params: { token } });
  }
}