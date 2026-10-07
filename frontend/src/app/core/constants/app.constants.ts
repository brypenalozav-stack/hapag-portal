/** Shared application constants to avoid magic strings/numbers */

export const FILTER_ALL = 'ALL';

export const TAX_RATES = {
  CL: 0.19,
  BO: 0.13,
} as const;

export const REDIRECT_DELAY_MS = 2000;

export const VALIDATION = {
  PASSWORD_MIN_LENGTH: 8,
  LOGIN_PASSWORD_MIN_LENGTH: 6,
  NAME_MIN_LENGTH: 3,
} as const;

export const ROLES = {
  ADMIN: 'ADMIN',
  BA: 'BA',
  CLIENT: 'CLIENT',
  AGENT: 'AGENT',
} as const;

/** Roles de la consola operativa interna (ven los módulos internos). */
export const INTERNAL_ROLES = ['ADMIN', 'ADMINISTRADOR', 'COORDINADOR', 'SUPERVISOR', 'SUPERADMIN'] as const;

export const COUNTRIES = {
  CHILE: 'CL' as const,
  BOLIVIA: 'BO' as const,
};

export const API_ENDPOINTS = {
  AUTH_LOGIN: 'auth/login',
  AUTH_REGISTER: 'auth/register',
  AUTH_REGISTER_JOIN: 'auth/register/join',
  AUTH_LOGOUT: 'auth/logout',
  AUTH_REFRESH_TOKEN: 'auth/refresh-token',
  AUTH_FORGOT_PASSWORD: 'auth/forgot-password',
  AUTH_RESET_PASSWORD: 'auth/reset-password',
  BILLS_OF_LADING: 'bills-of-lading',
  PAYMENTS: 'payments',
  FAQS: 'faqs',
  WAREHOUSE_CHANGES: 'warehouse-changes',
  SERVICE_ORDERS: 'service-orders',
  RECEIPTS: 'receipts',
  CONFIG_TAX_RATES: 'config/tax-rates',
  CONFIG_CURRENCIES: 'config/currencies',
  CONFIG_PAYMENT_METHODS: 'config/payment-methods',
  CLIENTS_ME: 'clients/me',
  SHIPMENTS: 'shipments',
  ORGANIZATIONS_ME: 'organizations/me',
  ADMIN_ORGANIZATIONS: 'admin/organizations',
  ACCESS_MATRIX: 'access-matrix',
  ACCESS: 'access',
  // Fase 1, Ola C
  CHARGES: 'charges',
  DEMURRAGE: 'demurrage',
  EXCHANGE_RATES: 'exchange-rates',
  COMMERCIAL_CONDITIONS: 'organizations/me/commercial-conditions',
  TARIFFS: 'tariffs',
  INTERNAL_CHARGE_RULES: 'internal-charge-rules',
  // Fase 1, Ola D
  CART: 'cart',
  ACCOUNT_PAYMENTS: 'account-payments',
  INVOICES: 'invoices',
  PAYMENT_HISTORY: 'payment-history',
  PAYMENT_CONFIG: 'payment-config',
  PAYMENT_BLOCKS: 'payment-blocks',
  ADMIN_PAYMENTS: 'admin/payments',
  // Fase 1, Ola E
  DOCUMENTS: 'documents',
  // Fase 1, Ola F
  DASHBOARD: 'dashboard',
  SHIPMENT_PUBLICATION_RULES: 'shipment-publication-rules',
  CONFIG_DISPUTE_LINK: 'config/dispute-link',
  /** Portal de devoluciones y tarifarios oficiales del país (Tarifas locales, Devoluciones). */
  CONFIG_EXTERNAL_LINKS: 'config/external-links',
  // Cierre de Fase 1: flags de funcionalidades (Fase 2 apagada por defecto).
  CONFIG_FEATURES: 'config/features',
  ASSISTANT: 'assistant',
  DANGEROUS_GOODS: 'dangerous-goods',
  // Fase 2, Ola G
  SERVICE_REQUESTS: 'service-requests',
  ADMIN_SERVICE_REQUESTS: 'admin/service-requests',
  SERVICE_DEFINITIONS: 'service-definitions',
  // Fase 2, Ola H
  ACCOUNT_STATEMENT: 'account-statement',
  REINVOICING: 'reinvoicing',
  // Fase 2, Ola I
  NOTIFICATIONS: 'notifications',
  ANNOUNCEMENTS: 'announcements',
  ADMIN_ANNOUNCEMENTS: 'admin/announcements',
  GUIDES: 'guides',
  ADMIN_GUIDES: 'admin/guides',
  ADMIN_OVERVIEW: 'admin/overview',
  ADMIN_IMPERSONATION: 'admin/impersonation',
  IMPERSONATION: 'impersonation',
  ADMIN_REPORTS: 'admin/reports',
  ADMIN_COUNTER: 'admin/counter',
  ADMIN_ORGANIZATION_LINKS: 'admin/organization-links',
  AUTH_RESEND_PRE_CREATED_INVITATION: 'auth/register/pre-created/resend-invitation',
  // Fase 2, Ola J
  ADMIN_API_CLIENTS: 'admin/api-clients',
} as const;

/**
 * Permisos del claim `permission` del JWT (Fase 1, Ola A). Solo deciden qué se muestra;
 * el servidor los vuelve a exigir en cada endpoint.
 */
export const PERMISSIONS = {
  /** Gestionar los usuarios de la propia organización (M1-02). */
  MANAGE_ORGANIZATION_USERS: 'org.users.manage',
  /** Aprobar o rechazar solicitudes de vinculación (M1-08). */
  APPROVE_JOIN_REQUESTS: 'org.requests.approve',
  /** Validar organizaciones nuevas (M8-04). */
  REVIEW_ORGANIZATIONS: 'organizations.review',
  /** Control con AR y asignación del Match Code (M8-04). */
  CHECK_ORGANIZATIONS_AR: 'organizations.ar-check',
  /** Administrar la matriz base de accesos (M1-11). */
  MANAGE_ACCESS_MATRIX: 'access-matrix.manage',
  /** Otorgar, editar y revocar accesos a terceros, defaults, acceso abierto y ampliaciones (M1-12 a M1-24). */
  MANAGE_THIRD_PARTY_ACCESS: 'org.access.manage',
  /** Mantenedores internos: tarifas (M8-01) y reglas internas de cobro (M3-04, M3-16). */
  MANAGE_MAINTAINERS: 'maintainers.manage',
  /** Finanzas: operaciones detenidas, anulación de boletas emitidas y conciliación (M5-02, NF-03, NF-04). */
  PAYMENTS_FINANCE: 'payments.finance',
  /** Ventanas de bloqueo de pagos por horario (M8-07). */
  MANAGE_PAYMENT_BLOCKS: 'payment-blocks.manage',
  /** Bandeja interna de solicitudes de servicios on demand (ED, Customer Service; Fase 2, Ola G). */
  PROCESS_SERVICE_REQUESTS: 'service-requests.process',
  /** Área de administración unificada (Fase 2, Ola I, M8-05). */
  ADMIN_AREA: 'admin-area.access',
  /** Vista como cliente (M8-08): solo el Administrador interno. */
  USE_IMPERSONATION: 'impersonation.use',
  /** Comunicados masivos (M1-26). */
  MANAGE_ANNOUNCEMENTS: 'announcements.manage',
  /** Counter Bolivia/Ultramar (M8-09). */
  MANAGE_COUNTER: 'counter.manage',
  /** Reportería de transacciones y excepciones (M9-01). */
  VIEW_TRANSACTIONS_REPORT: 'transactions-report.view',
  /** Clientes, claves y bitácora del canal Web Service (Fase 2, Ola J, M3-17). */
  MANAGE_API_CLIENTS: 'api-clients.manage',
} as const;
