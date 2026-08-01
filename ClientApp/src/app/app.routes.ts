import { Routes } from '@angular/router';
import { ProductListComponent } from './features/catalog/product-list/product-list';
import { ProductDetailComponent } from './features/catalog/product-detail/product-detail';
import { CartPageComponent } from './features/cart/cart-page/cart-page';
import { CheckoutPageComponent } from './features/checkout/checkout-page/checkout-page';
import { OrderConfirmationComponent } from './features/checkout/order-confirmation/order-confirmation';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';

export const routes: Routes = [
  { path: '', component: ProductListComponent },
  { path: 'products/:id', component: ProductDetailComponent },
  { path: 'cart', component: CartPageComponent },
  { path: 'checkout', component: CheckoutPageComponent },
  { path: 'order-confirmation/:id', component: OrderConfirmationComponent },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
];