import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { sembrarSesion } from '../fixtures/session';

/** Logos de los medios de pago (app-payment-logo): decorativos, cargan y no cambian el nombre de cada opción. */
test('cada medio del carro muestra su logo sin alterar el nombre accesible de la opción', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/cart');
  const clp = page.getByTestId('cart-group-CL-CLP');
  await expect(clp.getByRole('radio', { name: 'Khipu (En línea)' })).toBeVisible();

  const logos = clp.locator('[data-testid="payment-logo"] img');
  await expect(logos.first()).toBeVisible();
  const cargados = await logos.evaluateAll((imgs) => imgs.map((i) => (i as HTMLImageElement).complete && (i as HTMLImageElement).naturalWidth > 0));
  expect(cargados.length).toBeGreaterThan(0);
  expect(cargados.every(Boolean)).toBe(true);
  for (const alt of await logos.evaluateAll((imgs) => imgs.map((i) => i.getAttribute('alt')))) expect(alt).toBe('');
});
