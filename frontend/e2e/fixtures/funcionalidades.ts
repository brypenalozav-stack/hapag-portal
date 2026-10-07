import { Page, Route } from '@playwright/test';
import type { FeatureName } from '../../src/app/core/services/feature.service';

/**
 * Flags de funcionalidades (GET /config/features) del cierre de Fase 1: los mismos valores por defecto que el backend
 * (Fase 2 apagada, salvo la carta de liberación y Counter).
 */
export type Funcionalidades = Partial<Record<FeatureName, boolean>>;

export const FUNCIONALIDADES_POR_DEFECTO: Readonly<Record<FeatureName, boolean>> = {
  ContactLists: false,
  Carriers: false,
  ParentCompany: false,
  NotificationsInbox: false,
  Announcements: false,
  GuideMode: false,
  OnDemandServices: false,
  ServiceOrdersPage: false,
  WarehouseHistory: false,
  Reinvoicing: false,
  ApiClients: false,
  GateOutAdvance: false,
  DepositProofs: false,
  CreditImputation: false,
  FreightCertificate: false,
  ReleaseLetter: true,
  AccountStatement: false,
  AdminHome: false,
  Impersonation: false,
  Counter: true,
  TransactionReports: false,
  AssistantDelivery: false,
  DocumentaryDeadlines: false,
};

/** Todos los flags encendidos: las pruebas de las funciones de Fase 2 (olas G a J). */
export const FASE2: Funcionalidades = Object.fromEntries(
  Object.keys(FUNCIONALIDADES_POR_DEFECTO).map((n) => [n, true]),
) as Funcionalidades;

/** Respuesta de GET /config/features: los valores por defecto con los indicados encima. */
export function funcionalidades(activas: Funcionalidades = {}): Record<FeatureName, boolean> {
  return { ...FUNCIONALIDADES_POR_DEFECTO, ...activas };
}

/**
 * Enciende flags para la página (por defecto, todos los de Fase 2). Se registra después de `simularApi`, así que tiene
 * prioridad sobre su respuesta; llamarlo antes de la primera navegación (los flags se leen una vez al iniciar la app).
 */
export async function habilitarFuncionalidades(page: Page, activas: Funcionalidades = FASE2): Promise<void> {
  await page.route('**/api/v1/config/features', async (route: Route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(funcionalidades(activas)) });
  });
}
