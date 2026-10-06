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
} as const;
