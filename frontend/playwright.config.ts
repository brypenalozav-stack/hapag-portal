import { defineConfig, devices } from '@playwright/test';

/**
 * Pruebas de extremo a extremo del frontend (Fase 5a): accesibilidad con axe sobre las
 * pantallas principales. El backend no se levanta: e2e/fixtures/api-mocks.ts responde /api/v1/**.
 *
 * La aplicación se prueba compilada (configuración `e2e` de angular.json: optimizada como producción, con
 * apiUrl '/api/v1'), no con `ng serve`, cuyo servidor de desarrollo recompilaba y recargaba la página. La
 * fixture automática de e2e/fixtures/app.ts entrega esos archivos desde memoria (respaldo SPA a index.html):
 * el navegador no abre conexiones a localhost, que en Windows quedaban esperando ~19 s de vez en cuando y
 * hacían fallar page.goto con net::ERR_ABORTED ("frame was detached"). El servidor estático de
 * e2e/serve-static.mjs sirve la misma compilación en :4300 para la espera del webServer y para revisarla a mano.
 */
export default defineConfig({
  testDir: 'e2e',
  // Cada prueba abre su propia página con su backend simulado (page.route) y la aplicación en memoria, así que se
  // reparten entre varios workers también las de un mismo archivo (pantallas.a11y.spec.ts reúne la mayoría).
  fullyParallel: true,
  workers: 4,
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
    // Compila una vez (con la caché de Angular tarda segundos) y sirve dist/e2e/browser en :4300.
    command: 'npx ng build --configuration e2e && node e2e/serve-static.mjs 4300',
    url: 'http://localhost:4300',
    reuseExistingServer: !process.env['CI'],
    timeout: 300_000,
    stdout: 'ignore',
    stderr: 'pipe',
  },
});
