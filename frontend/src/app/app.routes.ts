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
    data: { preload: true },
  },
  // Listado y detalle de embarques (M2-06, M2-07); reemplazan a la consulta de BL.
  {
    path: 'shipments',
    loadComponent: () =>
      import('./features/shipments/shipment-list/shipment-list').then((m) => m.ShipmentListComponent),
    canActivate: [authGuard],
    data: { preload: true },
  },
  {
    path: 'shipments/:blNumber',
    loadComponent: () =>
      import('./features/shipments/shipment-detail/shipment-detail').then((m) => m.ShipmentDetailComponent),
    canActivate: [authGuard],
    data: { preload: true },
  },
  // Ola E: repositorio documental del embarque en su propia página (M6-09), para enlaces directos.
  {
    path: 'shipments/:blNumber/documents',
    loadComponent: () =>
      import('./features/documents/shipment-documents-page').then((m) => m.ShipmentDocumentsPageComponent),
    canActivate: [authGuard],
  },
  // Fase 2, Ola J: solicitud y seguimiento de la carta de liberación y desconsolidado de Bolivia (M6-08).
  {
    path: 'shipments/:blNumber/release-letter',
    loadComponent: () =>
      import('./features/documents/release-letter/release-letter-form').then((m) => m.ReleaseLetterFormComponent),
    canActivate: [authGuard],
  },
  {
    path: 'release-letters/:id',
    loadComponent: () =>
      import('./features/documents/release-letter/release-letter-detail').then((m) => m.ReleaseLetterDetailComponent),
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
  // Ola D: carro unificado por moneda (M5-01, M5-08, M5-09), pago desde la cuenta para clientes con
  // crédito (M5-07), resultado del pago (NF-02, NF-12), facturas (M7-01) e historial de pagos (M7-02).
  // El listado de pagos, los comprobantes y el pago por BL anteriores se retiraron: sus rutas redirigen.
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart').then((m) => m.CartComponent),
    canActivate: [authGuard],
    data: { preload: true },
  },
  {
    path: 'account-payments',
    loadComponent: () =>
      import('./features/account-payments/account-payments').then((m) => m.AccountPaymentsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'payments/:id/result',
    loadComponent: () =>
      import('./features/payments/payment-result/payment-result').then((m) => m.PaymentResultComponent),
    canActivate: [authGuard],
  },
  {
    path: 'invoices',
    loadComponent: () => import('./features/invoices/invoices').then((m) => m.InvoicesComponent),
    canActivate: [authGuard],
    data: { preload: true },
  },
  // Fase 2, Ola H: estado de cuenta en línea con pago por carro o forma de pago por ítem con crédito (M7-03, M5-10).
  {
    path: 'account-statement',
    loadComponent: () =>
      import('./features/account-statement/account-statement').then((m) => m.AccountStatementComponent),
    canActivate: [authGuard],
    data: { preload: true },
  },
  // Fase 2, Ola H: refacturación IAO con pérdida de IVA (M3-11). La aceptación de la nueva razón social es pública: se
  // abre desde el enlace de un solo uso enviado a su correo, sin sesión.
  {
    path: 'reinvoicing/new',
    loadComponent: () =>
      import('./features/reinvoicing/reinvoicing-new/reinvoicing-new').then((m) => m.ReinvoicingNewComponent),
    canActivate: [authGuard],
  },
  {
    path: 'reinvoicing/acceptance/:token',
    loadComponent: () =>
      import('./features/reinvoicing/reinvoicing-acceptance/reinvoicing-acceptance').then((m) => m.ReinvoicingAcceptanceComponent),
  },
  {
    path: 'reinvoicing/:id',
    loadComponent: () =>
      import('./features/reinvoicing/reinvoicing-detail/reinvoicing-detail').then((m) => m.ReinvoicingDetailComponent),
    canActivate: [authGuard],
  },
  {
    path: 'payment-history',
    loadComponent: () =>
      import('./features/payment-history/payment-history').then((m) => m.PaymentHistoryComponent),
    canActivate: [authGuard],
    data: { preload: true },
  },
  {
    path: 'payment-history/:id',
    loadComponent: () =>
      import('./features/payment-history/payment-history-detail').then((m) => m.PaymentHistoryDetailComponent),
    canActivate: [authGuard],
  },
  { path: 'payments', redirectTo: '/payment-history', pathMatch: 'full' },
  { path: 'payments/new/:blId', redirectTo: '/cart' },
  { path: 'receipts', redirectTo: '/payment-history', pathMatch: 'full' },
  // Ola C: cargos con las reglas de Nexus (M4-01 a M4-04, M3-01, M5-05); reemplazan a /local-charges.
  {
    path: 'charges',
    loadComponent: () =>
      import('./features/charges/charges').then((m) => m.ChargesComponent),
    canActivate: [authGuard],
  },
  {
    path: 'charges/:blNumber',
    loadComponent: () =>
      import('./features/charges/charges').then((m) => m.ChargesComponent),
    canActivate: [authGuard],
  },
  { path: 'local-charges', redirectTo: '/charges', pathMatch: 'full' },
  { path: 'local-charges/:blNumber', redirectTo: '/charges/:blNumber' },
  // Demurrage por estado del BL (M3-18, M3-02, M3-16).
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
  // Cambio de almacén gratuito o tarifado y solicitud masiva (M3-04, M3-05).
  {
    path: 'warehouse',
    loadComponent: () =>
      import('./features/warehouse/warehouse').then((m) => m.WarehouseComponent),
    canActivate: [authGuard],
  },
  // Avance de una solicitud masiva de cambio de almacén (M3-05, NF-19).
  {
    path: 'warehouse/bulk/:id',
    loadComponent: () =>
      import('./features/warehouse/warehouse-bulk-progress/warehouse-bulk-progress').then(
        (m) => m.WarehouseBulkProgressComponent,
      ),
    canActivate: [authGuard],
  },
  // Fase 2, Ola G: historial y trazabilidad del cambio de almacén (M3-06).
  {
    path: 'warehouse/history',
    loadComponent: () =>
      import('./features/warehouse/warehouse-history/warehouse-history').then((m) => m.WarehouseHistoryComponent),
    canActivate: [authGuard],
  },
  {
    path: 'warehouse/history/:id',
    loadComponent: () =>
      import('./features/warehouse/warehouse-history/warehouse-history-detail').then((m) => m.WarehouseHistoryDetailComponent),
    canActivate: [authGuard],
  },
  // Fase 2, Ola G: servicios on demand (M2-03, M2-04, M3-07 a M3-15): mis solicitudes, nueva solicitud y detalle.
  {
    path: 'service-requests',
    loadComponent: () =>
      import('./features/service-requests/service-request-list/service-request-list').then((m) => m.ServiceRequestListComponent),
    canActivate: [authGuard],
  },
  {
    path: 'service-requests/new',
    loadComponent: () =>
      import('./features/service-requests/service-request-form/service-request-form').then((m) => m.ServiceRequestFormComponent),
    canActivate: [authGuard],
  },
  {
    path: 'service-requests/:id',
    loadComponent: () =>
      import('./features/service-requests/service-request-detail/service-request-detail').then((m) => m.ServiceRequestDetailComponent),
    canActivate: [authGuard],
  },
  {
    path: 'service-orders',
    loadComponent: () =>
      import('./features/service-orders/service-orders').then((m) => m.ServiceOrdersComponent),
    canActivate: [authGuard],
  },
  // Ola F: solicitud masiva de TATC por localidad (M2-09) y buscador de mercancías peligrosas (M10-06).
  {
    path: 'tatc',
    loadComponent: () => import('./features/tatc/tatc-bulk').then((m) => m.TatcBulkComponent),
    canActivate: [authGuard],
  },
  {
    path: 'dangerous-goods',
    loadComponent: () =>
      import('./features/dangerous-goods/dangerous-goods').then((m) => m.DangerousGoodsComponent),
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
  // Fase 2, Ola I: preferencias de correo de la bandeja (M1-25) y comunicados vigentes (M1-26).
  {
    path: 'notifications/preferences',
    loadComponent: () =>
      import('./features/notifications/notification-preferences').then((m) => m.NotificationPreferencesComponent),
    canActivate: [authGuard],
  },
  {
    path: 'announcements',
    loadComponent: () =>
      import('./features/announcements/announcements').then((m) => m.AnnouncementsComponent),
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
  // Ola C: mantenedores internos de tarifas (M8-01, NF-15) y reglas internas de cobro (M3-04, M3-16).
  // Clientes con crédito y exenciones se leen de Nexus (M8-02): ya no tienen mantenedor en el portal.
  {
    path: 'admin/tariffs',
    loadComponent: () =>
      import('./features/admin/tariffs/tariffs').then((m) => m.TariffsComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/tariffs/new',
    loadComponent: () =>
      import('./features/admin/tariffs/tariff-editor').then((m) => m.TariffEditorComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/tariffs/:id',
    loadComponent: () =>
      import('./features/admin/tariffs/tariff-editor').then((m) => m.TariffEditorComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/internal-charge-rules',
    loadComponent: () =>
      import('./features/admin/internal-rules/internal-rules').then((m) => m.InternalRulesComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  // Ola D: configuración de pagos (M5-03, M5-04), ventanas de bloqueo (M8-07) y Finanzas (M5-02, NF-03, NF-04).
  {
    path: 'admin/payment-currencies',
    loadComponent: () =>
      import('./features/admin/payment-config/payment-currencies').then((m) => m.PaymentCurrenciesComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/payment-methods',
    loadComponent: () =>
      import('./features/admin/payment-config/payment-methods').then((m) => m.PaymentMethodsComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/payment-blocks',
    loadComponent: () =>
      import('./features/admin/payment-config/payment-blocks').then((m) => m.PaymentBlocksComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_PAYMENT_BLOCKS)],
  },
  {
    path: 'admin/payments-finance',
    loadComponent: () =>
      import('./features/admin/payment-config/payments-finance').then((m) => m.PaymentsFinanceComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.PAYMENTS_FINANCE)],
  },
  // Fase 2, Ola H: comprobantes de depósito por verificar (M5-06), anticipos y su cruce con las facturas (M7-03, M3-19,
  // NF-04) y conceptos imputables a la línea de crédito con su registro de cambios (M5-10, NF-15).
  {
    path: 'admin/payments/deposit-proofs',
    loadComponent: () =>
      import('./features/admin/payment-config/deposit-proof-queue').then((m) => m.DepositProofQueueComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.PAYMENTS_FINANCE)],
  },
  {
    path: 'admin/payments/settlements',
    loadComponent: () =>
      import('./features/admin/payment-config/settlements').then((m) => m.SettlementsComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.PAYMENTS_FINANCE)],
  },
  {
    path: 'admin/credit-imputation-rules',
    loadComponent: () =>
      import('./features/admin/payment-config/credit-imputation-rules').then((m) => m.CreditImputationRulesComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  // Ola F: reglas de publicación por DIFU (M2-01), base de conocimiento y casillas del asistente (M10-02) y
  // carga de la base de referencia de mercancías peligrosas (M10-06), con registro de cambios (NF-15).
  {
    path: 'admin/publication-rules',
    loadComponent: () =>
      import('./features/admin/publication-rules/publication-rules').then((m) => m.PublicationRulesComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/assistant-knowledge',
    loadComponent: () =>
      import('./features/admin/assistant-knowledge/assistant-knowledge').then((m) => m.AssistantKnowledgeComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/assistant-mailboxes',
    loadComponent: () =>
      import('./features/admin/assistant-mailboxes/assistant-mailboxes').then((m) => m.AssistantMailboxesComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/dangerous-goods',
    loadComponent: () =>
      import('./features/admin/dangerous-goods-import/dangerous-goods-import').then((m) => m.DangerousGoodsImportComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  // Fase 2, Ola G: bandeja interna de solicitudes de servicios (ED, Customer Service) y mantenedor de definiciones
  // de servicios on demand con su registro de cambios (M2-03, M2-04, NF-15).
  {
    path: 'admin/service-requests',
    loadComponent: () =>
      import('./features/admin/service-requests/service-request-queue').then((m) => m.ServiceRequestQueueComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.PROCESS_SERVICE_REQUESTS)],
  },
  {
    path: 'admin/service-requests/:id',
    loadComponent: () =>
      import('./features/admin/service-requests/service-request-review').then((m) => m.ServiceRequestReviewComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.PROCESS_SERVICE_REQUESTS)],
  },
  {
    path: 'admin/service-definitions',
    loadComponent: () =>
      import('./features/admin/service-definitions/service-definitions').then((m) => m.ServiceDefinitionsComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/service-definitions/new',
    loadComponent: () =>
      import('./features/admin/service-definitions/service-definition-editor').then((m) => m.ServiceDefinitionEditorComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/service-definitions/:id',
    loadComponent: () =>
      import('./features/admin/service-definitions/service-definition-editor').then((m) => m.ServiceDefinitionEditorComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  // Fase 2, Ola I: área de administración unificada (M8-05), comunicados (M1-26), guías (M1-27), vista como cliente
  // (M8-08), reportería de transacciones y excepciones (M9-01), Counter (M8-09) y vinculaciones con la matriz (M1-21).
  {
    path: 'admin',
    pathMatch: 'full',
    loadComponent: () => import('./features/admin/admin-home/admin-home').then((m) => m.AdminHomeComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.ADMIN_AREA)],
  },
  {
    path: 'admin/announcements',
    loadComponent: () =>
      import('./features/admin/announcements/announcements-admin').then((m) => m.AnnouncementsAdminComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_ANNOUNCEMENTS)],
  },
  {
    path: 'admin/guides',
    loadComponent: () => import('./features/admin/guides/guides-admin').then((m) => m.GuidesAdminComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_MAINTAINERS)],
  },
  {
    path: 'admin/impersonation',
    loadComponent: () => import('./features/admin/impersonation/impersonation').then((m) => m.ImpersonationComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.USE_IMPERSONATION)],
  },
  {
    path: 'admin/reports/transactions',
    loadComponent: () =>
      import('./features/admin/transaction-reports/transactions-report').then((m) => m.TransactionsReportComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.VIEW_TRANSACTIONS_REPORT)],
  },
  {
    path: 'admin/reports/exceptions',
    loadComponent: () =>
      import('./features/admin/transaction-reports/exceptions-report').then((m) => m.ExceptionsReportComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.VIEW_TRANSACTIONS_REPORT)],
  },
  {
    path: 'admin/counter',
    loadComponent: () => import('./features/admin/counter/counter').then((m) => m.CounterComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_COUNTER)],
  },
  {
    path: 'admin/organization-links',
    loadComponent: () =>
      import('./features/admin/organization-links/organization-links').then((m) => m.OrganizationLinksComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.REVIEW_ORGANIZATIONS)],
  },
  // Fase 2, Ola J: clientes, claves y bitácora del canal Web Service (M3-17).
  {
    path: 'admin/api-clients',
    loadComponent: () => import('./features/admin/api-clients/api-clients').then((m) => m.ApiClientsComponent),
    canActivate: [authGuard, internalGuard, permissionGuard(PERMISSIONS.MANAGE_API_CLIENTS)],
  },
  { path: '**', redirectTo: '/dashboard' },
];
