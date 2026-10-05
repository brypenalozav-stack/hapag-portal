import { Page } from '@playwright/test';
import type { Client } from '../../src/app/core/models/client.model';
import type { OrganizationSummary } from '../../src/app/core/models/organization.model';

/** Usuario cliente de Chile con el que se siembra la sesión de las pantallas autenticadas. */
export const USUARIO_PRUEBA: Client = {
  id: '7d1c2b3a-0000-4000-8000-000000000001',
  name: 'Importadora Andes SpA',
  email: 'cliente.prueba@example.com',
  taxId: '76.123.456-7',
  phone: '+56 2 2345 6789',
  country: 'CL',
  type: 'CLIENT',
  role: 'USER',
  isActive: true,
  createdAt: '2026-01-15T12:00:00Z',
};

/** Organización del usuario de prueba: aprobada, opera en Chile y Bolivia, perfil administrador (M1-02, M1-04). */
export const ORGANIZACION_PRUEBA: OrganizationSummary = {
  id: '0a1b2c3d-0000-4000-8000-000000000001',
  name: USUARIO_PRUEBA.name,
  taxId: USUARIO_PRUEBA.taxId,
  taxIdType: 'RUT',
  country: 'CL',
  organizationType: 'Customer',
  status: 'Approved',
  matchCode: 'CLANDES01',
  operatingCountries: ['CL', 'BO'],
  membershipStatus: 'Active',
  profile: 'OrgAdmin',
  canOperate: true,
};

/** Permisos del perfil administrador de organización (claim `permission` del JWT). */
export const PERMISOS_ORG_ADMIN = ['org.users.manage', 'org.requests.approve', 'shipments.operate'];

/** Administrador interno de Hapag-Lloyd (M8-06). */
export const USUARIO_ADMIN: Client = {
  id: '7d1c2b3a-0000-4000-8000-000000000099',
  name: 'Hapag-Lloyd Chile',
  email: 'admin@hapag-lloyd.cl',
  taxId: '96.000.000-0',
  phone: '+56 2 2000 0000',
  country: 'CL',
  type: 'CLIENT',
  role: 'ADMIN',
  isActive: true,
  createdAt: '2025-01-01T12:00:00Z',
};

export const ORGANIZACION_ADMIN: OrganizationSummary = {
  id: '0a1b2c3d-0000-4000-8000-000000000099',
  name: USUARIO_ADMIN.name,
  taxId: USUARIO_ADMIN.taxId,
  taxIdType: 'RUT',
  country: 'CL',
  organizationType: 'Internal',
  status: 'Approved',
  matchCode: null,
  operatingCountries: ['CL'],
  membershipStatus: 'Active',
  profile: null,
  canOperate: true,
};

/** Permisos internos de la Ola A (M8-04, M8-06, M1-11). */
export const PERMISOS_ADMIN = [
  'shipments.view-all',
  'organizations.review',
  'organizations.ar-check',
  'access-matrix.manage',
];

export type Idioma = 'es' | 'en';

/** Idiomas de la interfaz (Q8): las pruebas de pantallas se repiten en cada uno. */
export const IDIOMAS: readonly Idioma[] = ['es', 'en'];

export interface OpcionesSesion {
  /** Idioma sembrado en `hl_lang` (lo lee LocaleService al arrancar). */
  lang?: Idioma;
  /** Usuario de la sesión; por defecto, el cliente de prueba. */
  usuario?: Client;
  /** Organización de la sesión (`hl_organization`); por defecto, la del cliente de prueba. */
  organizacion?: OrganizationSummary | null;
  /** Claims `permission` del JWT; por defecto, los del administrador de organización. */
  permisos?: string[];
}

/**
 * JWT de prueba sin firma válida: la interfaz solo lee el claim `permission` para mostrar u
 * ocultar opciones (el backend está simulado).
 */
export function tokenDePrueba(permisos: string[]): string {
  const codificar = (valor: unknown) => Buffer.from(JSON.stringify(valor)).toString('base64url');
  return `${codificar({ alg: 'HS256', typ: 'JWT' })}.${codificar({ sub: 'prueba', permission: permisos })}.firma-de-prueba`;
}

/** Siembra el idioma en localStorage (`hl_lang`) antes de que cargue la aplicación. */
export async function sembrarIdioma(page: Page, lang: Idioma): Promise<void> {
  await page.addInitScript((idioma) => {
    localStorage.setItem('hl_lang', idioma);
  }, lang);
}

/**
 * Siembra la sesión en localStorage antes de que cargue la aplicación, con las mismas claves
 * que usa AuthService (`hl_token`, `hl_refresh_token`, `hl_user`, `hl_organization`) y, si se
 * indica, el idioma (`hl_lang`).
 */
export async function sembrarSesion(page: Page, opciones: OpcionesSesion = {}): Promise<void> {
  const organizacion = opciones.organizacion === undefined ? ORGANIZACION_PRUEBA : opciones.organizacion;
  await page.addInitScript(
    ({ usuario, organizacion, token }) => {
      localStorage.setItem('hl_token', token);
      localStorage.setItem('hl_refresh_token', 'refresh-de-prueba');
      localStorage.setItem('hl_user', JSON.stringify(usuario));
      if (organizacion) localStorage.setItem('hl_organization', JSON.stringify(organizacion));
    },
    {
      usuario: opciones.usuario ?? USUARIO_PRUEBA,
      organizacion,
      token: tokenDePrueba(opciones.permisos ?? PERMISOS_ORG_ADMIN),
    },
  );
  if (opciones.lang) {
    await sembrarIdioma(page, opciones.lang);
  }
}

/** Sesión del administrador interno de Hapag-Lloyd con los permisos de la Ola A. */
export async function sembrarSesionAdmin(page: Page, lang?: Idioma): Promise<void> {
  await sembrarSesion(page, {
    lang,
    usuario: USUARIO_ADMIN,
    organizacion: ORGANIZACION_ADMIN,
    permisos: PERMISOS_ADMIN,
  });
}
