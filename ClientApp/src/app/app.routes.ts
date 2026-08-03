import { Routes } from '@angular/router';
import { ProductListComponent } from './features/catalog/product-list/product-list';
import { ProductDetailComponent } from './features/catalog/product-detail/product-detail';
import { CartPageComponent } from './features/cart/cart-page/cart-page';
import { CheckoutPageComponent } from './features/checkout/checkout-page/checkout-page';
import { OrderConfirmationComponent } from './features/checkout/order-confirmation/order-confirmation';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { AdminDashboard } from './features/admin/admin-dashboard/admin-dashboard';
import { AdminProducts } from './features/admin/admin-products/admin-products';
import { AdminCategories } from './features/admin/admin-categories/admin-categories';
import { AdminOrders } from './features/admin/admin-orders/admin-orders';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  { path: '', component: ProductListComponent },
  { path: 'products/:id', component: ProductDetailComponent },
  { path: 'cart', component: CartPageComponent },
  { path: 'checkout', component: CheckoutPageComponent },
  { path: 'order-confirmation/:id', component: OrderConfirmationComponent },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'admin', component: AdminDashboard, canActivate: [adminGuard] },
  { path: 'admin/products', component: AdminProducts, canActivate: [adminGuard] },
  { path: 'admin/categories', component: AdminCategories, canActivate: [adminGuard] },
  { path: 'admin/orders', component: AdminOrders, canActivate: [adminGuard] },
];