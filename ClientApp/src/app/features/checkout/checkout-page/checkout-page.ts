import { Component, OnInit, AfterViewInit, inject, signal, effect, ElementRef, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { loadStripe, Stripe, StripeElements, StripeCardElement, StripeElementStyle } from '@stripe/stripe-js';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { PaymentService } from '../../../core/services/payment.service';
import { ThemeService } from '../../../core/services/theme.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-checkout-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule
  ],
  templateUrl: './checkout-page.html',
  styleUrl: './checkout-page.css'
})
export class CheckoutPageComponent implements OnInit, AfterViewInit {
  cartService = inject(CartService);
  private orderService = inject(OrderService);
  private paymentService = inject(PaymentService);
  private themeService = inject(ThemeService);
  private router = inject(Router);

  cardElementRef = viewChild<ElementRef>('cardElement');

  email = signal('');
  loading = signal(false);
  error = signal<string | null>(null);
  cardReady = signal(false);

  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;
  private cardElement: StripeCardElement | null = null;

  constructor() {
    // Stripe's card element renders in its own iframe and doesn't inherit page CSS,
    // so it needs its colors pushed in explicitly and re-pushed on theme changes.
    effect(() => {
      this.themeService.isDark();
      this.cardElement?.update({ style: this.buildCardStyle() });
    });
  }

  ngOnInit(): void {
    if (this.cartService.items().length === 0) {
      this.router.navigate(['/cart']);
    }
  }

  async ngAfterViewInit(): Promise<void> {
    this.stripe = await loadStripe(environment.stripePublishableKey);
    if (!this.stripe) {
      this.error.set('Failed to load Stripe.');
      return;
    }

    this.elements = this.stripe.elements();
    this.cardElement = this.elements.create('card', { style: this.buildCardStyle() });

    const el = this.cardElementRef()?.nativeElement;
    if (el) {
      this.cardElement.mount(el);
      this.cardReady.set(true);
    }
  }

  private buildCardStyle(): StripeElementStyle {
    return {
      base: {
        color: this.resolveCssColor('--mat-sys-on-surface'),
        '::placeholder': { color: this.resolveCssColor('--mat-sys-on-surface-variant') }
      }
    };
  }

  // getComputedStyle().getPropertyValue() on a custom property returns its raw token
  // string (e.g. "light-dark(#1a1b1f, #e3e2e6)"), not a resolved color - light-dark()
  // only evaluates when used as an actual CSS property value. Applying it to a real
  // element's `color` and reading that back gives the browser's resolved rgb().
  private resolveCssColor(varName: string): string {
    const probe = document.createElement('span');
    probe.style.color = `var(${varName})`;
    document.body.appendChild(probe);
    const resolved = getComputedStyle(probe).color;
    probe.remove();
    return resolved;
  }

  async submitPayment(): Promise<void> {
    if (!this.email() || !this.stripe || !this.cardElement) return;

    this.loading.set(true);
    this.error.set(null);

    try {
      // Step 1: create the order
      const order = await this.orderService.createOrder({
        items: this.cartService.items().map(i => ({
          productId: i.product.id,
          quantity: i.quantity
        })),
        guestEmail: this.email()
      }).toPromise();

      if (!order) throw new Error('Order creation failed.');

      // Step 2: create the PaymentIntent
      const intentResponse = await this.paymentService.createPaymentIntent(order.id, order.accessToken).toPromise();
      if (!intentResponse) throw new Error('Payment setup failed.');

      // Step 3: confirm payment with Stripe
      const result = await this.stripe.confirmCardPayment(intentResponse.clientSecret, {
        payment_method: { card: this.cardElement }
      });

      if (result.error) {
        this.error.set(result.error.message ?? 'Payment failed.');
        this.releaseStock(order.id, order.accessToken);
        this.loading.set(false);
        return;
      }

      if (result.paymentIntent?.status === 'succeeded') {
        this.cartService.clear();
        this.router.navigate(['/order-confirmation', order.id], { queryParams: { token: order.accessToken } });
      } else {
        // Neither succeeded nor errored (e.g. still requires action). Stock was already
        // reserved at order creation, so release it instead of leaving the order stuck Pending.
        this.error.set('Payment could not be completed. Please try again.');
        this.releaseStock(order.id, order.accessToken);
      }
    } catch (err) {
      this.error.set('Something went wrong. Please try again.');
      console.error(err);
    } finally {
      this.loading.set(false);
    }
  }

  private releaseStock(orderId: number, accessToken: string): void {
    this.orderService.cancelOrder(orderId, accessToken).subscribe({
      error: (err) => console.error('Failed to release stock for cancelled order.', err)
    });
  }
}