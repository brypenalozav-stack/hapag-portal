import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register').then((m) => m.RegisterComponent),
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard').then((m) => m.DashboardComponent),
    canActivate: [authGuard],
  },
  {
    path: 'bills-of-lading',
    loadComponent: () =>
      import('./features/bill-of-lading/bl-list/bl-list').then((m) => m.BLListComponent),
    canActivate: [authGuard],
  },
  {
    path: 'bills-of-lading/:blNumber',
    loadComponent: () =>
      import('./features/bill-of-lading/bl-detail/bl-detail').then((m) => m.BLDetailComponent),
    canActivate: [authGuard],
  },
  {
    path: 'payments',
    loadComponent: () =>
      import('./features/payments/payment-list/payment-list').then((m) => m.PaymentListComponent),
    canActivate: [authGuard],
  },
  {
    path: 'payments/new/:blId',
    loadComponent: () =>
      import('./features/payments/payment-form/payment-form').then((m) => m.PaymentFormComponent),
    canActivate: [authGuard],
  },
  {
    path: 'local-charges',
    loadComponent: () =>
      import('./features/local-charges/local-charges').then((m) => m.LocalChargesComponent),
    canActivate: [authGuard],
  },
  {
    path: 'local-charges/:blNumber',
    loadComponent: () =>
      import('./features/local-charges/local-charges').then((m) => m.LocalChargesComponent),
    canActivate: [authGuard],
  },
  {
    path: 'demurrage',
    loadComponent: () =>
      import('./features/demurrage/demurrage').then((m) => m.DemurrageComponent),
    canActivate: [authGuard],
  },
  {
    path: 'demurrage/:blNumber',
    loadComponent: () =>
      import('./features/demurrage/demurrage').then((m) => m.DemurrageComponent),
    canActivate: [authGuard],
  },
  {
    path: 'warehouse',
    loadComponent: () =>
      import('./features/warehouse/warehouse').then((m) => m.WarehouseComponent),
    canActivate: [authGuard],
  },
  {
    path: 'service-orders',
    loadComponent: () =>
      import('./features/service-orders/service-orders').then((m) => m.ServiceOrdersComponent),
    canActivate: [authGuard],
  },
  {
    path: 'receipts',
    loadComponent: () =>
      import('./features/receipts/receipts').then((m) => m.ReceiptsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'profile',
    loadComponent: () =>
      import('./features/profile/profile').then((m) => m.ProfileComponent),
    canActivate: [authGuard],
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password').then((m) => m.ForgotPasswordComponent),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password').then((m) => m.ResetPasswordComponent),
  },
  {
    path: 'faq',
    loadComponent: () =>
      import('./features/faq/faq').then((m) => m.FAQComponent),
  },
  {
    path: 'admin/credit-clients',
    loadComponent: () =>
      import('./features/admin/credit-clients/credit-clients').then((m) => m.CreditClientsComponent),
    canActivate: [authGuard, adminGuard],
  },
  {
    path: 'admin/demurrage-exemptions',
    loadComponent: () =>
      import('./features/admin/demurrage-exemptions/demurrage-exemptions').then(
        (m) => m.DemurrageExemptionsComponent,
      ),
    canActivate: [authGuard, adminGuard],
  },
  { path: '**', redirectTo: '/dashboard' },
];
