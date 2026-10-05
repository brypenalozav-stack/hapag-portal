import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { internalGuard } from './core/guards/internal.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { PERMISSIONS } from './core/constants/app.constants';

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
    path: 'register/join',
    loadComponent: () =>
      import('./features/auth/join-organization/join-organization').then((m) => m.JoinOrganizationComponent),
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard').then((m) => m.DashboardComponent),
    canActivate: [authGuard],
  },
  // Listado y detalle de embarques (M2-06, M2-07); reemplazan a la consulta de BL.
  {
    path: 'shipments',
    loadComponent: () =>
      import('./features/shipments/shipment-list/shipment-list').then((m) => m.ShipmentListComponent),
    canActivate: [authGuard],
  },
  {
    path: 'shipments/:blNumber',
    loadComponent: () =>
      import('./features/shipments/shipment-detail/shipment-detail').then((m) => m.ShipmentDetailComponent),
    canActivate: [authGuard],
  },
  { path: 'bills-of-lading', redirectTo: '/shipments', pathMatch: 'full' },
  { path: 'bills-of-lading/:blNumber', redirectTo: '/shipments/:blNumber' },
  {
    path: 'organization',
    loadComponent: () =>
      import('./features/organization/organization').then((m) => m.OrganizationComponent),
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
    path: 'notifications',
    loadComponent: () =>
      import('./features/notifications/notifications').then((m) => m.NotificationsComponent),
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
  {
    path: 'admin/users',
    loadComponent: () => import('./features/admin/users/users').then((m) => m.UsersComponent),
    canActivate: [authGuard, adminGuard],
  },
  {
    path: 'admin/bl-import',
    loadComponent: () =>
      import('./features/admin/bl-import/bl-import').then((m) => m.BlImportComponent),
    canActivate: [authGuard, internalGuard],
  },
  {
    path: 'admin/customs',
    loadComponent: () =>
      import('./features/admin/customs/customs').then((m) => m.CustomsComponent),
    canActivate: [authGuard, internalGuard],
  },
  {
    path: 'admin/deadlines',
    loadComponent: () =>
      import('./features/admin/deadlines/deadlines').then((m) => m.DeadlinesComponent),
    canActivate: [authGuard, internalGuard],
  },
  {
    path: 'admin/audit',
    loadComponent: () =>
      import('./features/admin/audit/audit').then((m) => m.AuditComponent),
    canActivate: [authGuard, internalGuard],
  },
  {
    path: 'admin/reports',
    loadComponent: () =>
      import('./features/admin/reports/reports').then((m) => m.ReportsComponent),
    canActivate: [authGuard, internalGuard],
  },
  // Flujo interno de clientes nuevos y Match Code (M8-04) y matriz base de accesos (M1-11).
  {
    path: 'admin/organizations',
    loadComponent: () =>
      import('./features/admin/organizations/organizations').then((m) => m.AdminOrganizationsComponent),
    canActivate: [
      authGuard,
      internalGuard,
      permissionGuard(PERMISSIONS.REVIEW_ORGANIZATIONS, PERMISSIONS.CHECK_ORGANIZATIONS_AR),
    ],
  },
  {
    path: 'admin/organizations/:id',
    loadComponent: () =>
      import('./features/admin/organizations/organization-review').then((m) => m.OrganizationReviewComponent),
    canActivate: [
      authGuard,
      internalGuard,
      permissionGuard(PERMISSIONS.REVIEW_ORGANIZATIONS, PERMISSIONS.CHECK_ORGANIZATIONS_AR),
    ],
  },
  {
    path: 'admin/access-matrix',
    loadComponent: () =>
      import('./features/admin/access-matrix/access-matrix').then((m) => m.AccessMatrixComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_ACCESS_MATRIX)],
  },
  { path: '**', redirectTo: '/dashboard' },
];
