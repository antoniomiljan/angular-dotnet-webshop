import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { OrderService } from '../../../core/services/order.service';
import { Order } from '../../../shared/models/order.model';

const STATUSES = ['Pending', 'Paid', 'Shipped', 'Delivered', 'Cancelled'];

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, MatTableModule, MatSelectModule, MatFormFieldModule],
  templateUrl: './admin-orders.html',
  styleUrl: './admin-orders.css'
})
export class AdminOrders implements OnInit {
  private orderService = inject(OrderService);

  orders = signal<Order[]>([]);
  error = signal<string | null>(null);
  statuses = STATUSES;

  columns = ['id', 'email', 'total', 'status', 'createdAt'];

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.orderService.getAllOrders().subscribe({
      next: (data) => this.orders.set(data),
      error: () => this.error.set('Failed to load orders.')
    });
  }

  updateStatus(orderId: number, currentStatus: string, newStatus: string): void {
    const messages: Record<string, string> = {
      'Paid': 'This marks the order as paid WITHOUT verifying with Stripe. Only do this if you\'ve manually confirmed payment (e.g. offline payment). Continue?',
      'Shipped': 'Mark this order as shipped? This usually means it has actually left the building.',
      'Delivered': 'Mark this order as delivered? Confirm the customer has actually received it.',
      'Cancelled': 'Cancel this order? This does not automatically refund any payment already taken via Stripe.',
      'Pending': 'Revert this order back to Pending? This is unusual and should only be done to correct a mistake.'
    };

    const message = messages[newStatus] ?? `Change order status to ${newStatus}?`;

    if (!confirm(message)) {
      return;
    }

    this.orderService.updateOrderStatus(orderId, newStatus).subscribe({
      next: () => this.loadOrders(),
      error: (err) => this.error.set(err.error ?? 'Failed to update order status.')
    });
  }
}