// Verifica que el CSS compilado aplica los overrides de Bootstrap (DC4): --bs-primary debe ser
// #b84a00 y no el azul por defecto #0d6efd. Requiere un build previo (dist/hapag-portal/browser).
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const DIST = fileURLToPath(new URL('../dist/hapag-portal/browser', import.meta.url));
const ESPERADO = /--bs-primary:\s*#b84a00\b/i;
const PROHIBIDO = /--bs-primary:\s*#0d6efd\b/i;

if (!existsSync(DIST)) {
  console.error(`check:theme: no existe ${DIST}. Ejecute antes "npx ng build --configuration production".`);
  process.exit(1);
}

const hojas = readdirSync(DIST).filter((f) => /^styles-.*\.css$/.test(f));
if (hojas.length === 0) {
  console.error(`check:theme: no hay styles-*.css en ${DIST}.`);
  process.exit(1);
}

const css = hojas.map((f) => readFileSync(join(DIST, f), 'utf-8')).join('\n');
const errores = [];
if (!ESPERADO.test(css)) errores.push('falta --bs-primary: #b84a00 (los overrides de Bootstrap no se aplican)');
if (PROHIBIDO.test(css)) errores.push('aparece --bs-primary: #0d6efd (azul por defecto de Bootstrap)');

if (errores.length > 0) {
  console.error(`check:theme: ${hojas.join(', ')}`);
  for (const e of errores) console.error(`  ${e}`);
  process.exit(1);
}
console.log(`check:theme: ${hojas.join(', ')} aplica --bs-primary #b84a00.`);
