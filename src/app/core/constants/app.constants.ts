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

export const COUNTRIES = {
  CHILE: 'CL' as const,
  BOLIVIA: 'BO' as const,
};

export const API_ENDPOINTS = {
  AUTH_LOGIN: 'auth/login',
  AUTH_REGISTER: 'auth/register',
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
  ADMIN_CREDIT_CLIENTS: 'admin/credit-clients',
  ADMIN_DEMURRAGE_EXEMPTIONS: 'admin/demurrage-exemptions',
} as const;
