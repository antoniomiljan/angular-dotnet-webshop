import { Routes } from '@angular/router';
import { ProductListComponent } from './features/catalog/product-list/product-list.component';
import { ProductDetailComponent } from './features/catalog/product-detail/product-detail.component';
import { CartPageComponent } from './features/cart/cart-page/cart-page.component';
import { CheckoutPageComponent } from './features/checkout/checkout-page/checkout-page.component';
import { OrderConfirmationComponent } from './features/checkout/order-confirmation/order-confirmation.component';

export const routes: Routes = [
  { path: '', component: ProductListComponent },
  { path: 'products/:id', component: ProductDetailComponent },
  { path: 'cart', component: CartPageComponent },
  { path: 'checkout', component: CheckoutPageComponent },
  { path: 'order-confirmation/:id', component: OrderConfirmationComponent },
];