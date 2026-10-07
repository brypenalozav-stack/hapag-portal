import { defineConfig, devices } from '@playwright/test';

/**
 * Pruebas sobre archivos estáticos (file://) de docs/. testDir es '.' para que otras fases
 * agreguen sus specs en esta misma carpeta (por ejemplo, la Fase 7 con documentacion.spec.ts).
 */
export default defineConfig({
  testDir: '.',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
