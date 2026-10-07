import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '../fixtures/app';
import { simularApi } from '../fixtures/api-mocks';
import { sembrarSesion } from '../fixtures/session';

/** Logos de los medios de pago (app-payment-logo): decorativos, cargan y no cambian el nombre de cada opción. */
test('cada medio del carro muestra su logo sin alterar el nombre accesible de la opción', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/cart');
  const clp = page.getByTestId('cart-group-CL-CLP');
  await expect(clp.getByRole('radio', { name: 'Khipu, en línea' })).toBeVisible();

  const logos = clp.locator('[data-testid="payment-logo"] img');
  await expect(logos.first()).toBeVisible();
  const cargados = await logos.evaluateAll((imgs) => imgs.map((i) => (i as HTMLImageElement).complete && (i as HTMLImageElement).naturalWidth > 0));
  expect(cargados.length).toBeGreaterThan(0);
  expect(cargados.every(Boolean)).toBe(true);
  for (const alt of await logos.evaluateAll((imgs) => imgs.map((i) => i.getAttribute('alt')))) expect(alt).toBe('');
});

const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

/** Selector de medio de pago (app-payment-method-picker): tarjetas agrupadas por tipo, radios nativos. */
test('los medios del carro son tarjetas agrupadas por tipo; elegir una tarjeta marca su radio', async ({ page }) => {
  await simularApi(page);
  await sembrarSesion(page, { lang: 'es' });
  await page.goto('/cart');
  const clp = page.getByTestId('cart-group-CL-CLP');

  const enLinea = clp.getByRole('group', { name: 'Pago en línea' });
  const deposito = clp.getByRole('group', { name: 'Depósito o transferencia' });
  await expect(enLinea.getByRole('radio')).toHaveCount(2);
  await expect(deposito.getByRole('radio')).toHaveCount(1);
  await expect(deposito.getByRole('radio', { name: 'Depósito bancario con boleta, depósito' })).toBeVisible();
  // El tipo va en una pastilla traducida, no entre paréntesis junto al nombre.
  await expect(clp.getByTestId('payment-method-card-KHIPU')).toContainText('En línea · inmediato');
  await expect(clp.getByTestId('payment-method-card-DEPOSIT')).toContainText('Depósito · Finanzas confirma');
  await expect(clp.getByText('Khipu (En línea)')).toHaveCount(0);

  // Toda la tarjeta es la etiqueta: un clic en la descripción elige el medio.
  const khipu = clp.getByRole('radio', { name: 'Khipu, en línea' });
  await expect(khipu).not.toBeChecked();
  await clp.getByTestId('payment-method-card-KHIPU').getByText('Transferencia simplificada desde su banco.').click();
  await expect(khipu).toBeChecked();
  await expect(khipu).toHaveAccessibleDescription(/En línea · inmediato Transferencia simplificada desde su banco\./);
  // Las flechas recorren el grupo de radios, también entre grupos.
  await page.keyboard.press('ArrowDown');
  await expect(clp.getByRole('radio', { name: 'Botón Banco de Chile, en línea' })).toBeChecked();
  await expect(khipu).not.toBeChecked();

  // Vaciar queda en la cabecera, lejos del pago, y conserva su confirmación.
  await expect(clp.getByRole('button', { name: 'Vaciar el carro en CLP' })).toBeVisible();

  for (const tema of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: tema, reducedMotion: 'reduce' });
    const axe = await new AxeBuilder({ page }).include('main').withTags(TAGS).analyze();
    expect(axe.violations, `axe carro con medios de pago ${tema}`).toEqual([]);
  }
});
