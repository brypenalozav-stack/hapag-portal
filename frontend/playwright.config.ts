import { defineConfig, devices } from '@playwright/test';

/**
 * Pruebas de extremo a extremo del frontend (Fase 5a): accesibilidad con axe sobre las
 * pantallas principales. El backend no se levanta: e2e/fixtures/api-mocks.ts responde /api/v1/**.
 */
export default defineConfig({
  testDir: 'e2e',
  // Un solo worker: con varios navegadores en paralelo el servidor de desarrollo de `ng serve`
  // deja peticiones sin responder y page.goto agota el tiempo (verificado en Windows).
  workers: 1,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:4300',
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'npx ng serve --configuration development --port 4300 --open=false',
    url: 'http://localhost:4300',
    reuseExistingServer: !process.env['CI'],
    timeout: 180_000,
  },
});
