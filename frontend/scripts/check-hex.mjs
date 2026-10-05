// Verifica que no queden colores hexadecimales sueltos en src/ (.scss, .html, .ts).
// Los colores viven en src/styles/_tokens.scss (DC4); index.html queda exento por
// <meta name="theme-color">. Termina con código 1 si encuentra alguno.
import { readdirSync, readFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = fileURLToPath(new URL('..', import.meta.url));
const SRC = join(RAIZ, 'src');
const EXTENSIONES = ['.scss', '.html', '.ts'];
const EXCLUIDOS = new Set(['src/styles/_tokens.scss', 'src/index.html']);
const HEX = /#[0-9a-fA-F]{3,8}\b/g;

function* archivos(dir) {
  for (const entrada of readdirSync(dir, { withFileTypes: true })) {
    const ruta = join(dir, entrada.name);
    if (entrada.isDirectory()) {
      yield* archivos(ruta);
    } else if (EXTENSIONES.some((ext) => entrada.name.endsWith(ext))) {
      yield ruta;
    }
  }
}

const hallazgos = [];
for (const ruta of archivos(SRC)) {
  const rel = relative(RAIZ, ruta).split(sep).join('/');
  if (EXCLUIDOS.has(rel)) continue;
  readFileSync(ruta, 'utf-8')
    .split(/\r?\n/)
    .forEach((linea, i) => {
      for (const m of linea.matchAll(HEX)) {
        hallazgos.push(`${rel}:${i + 1}: ${m[0]}`);
      }
    });
}

if (hallazgos.length > 0) {
  console.error(`check:hex: ${hallazgos.length} color(es) hexadecimal(es) fuera de _tokens.scss:`);
  for (const h of hallazgos) console.error(`  ${h}`);
  console.error('Use un token (t.$hl-…) o una variable CSS (var(--hl-…)).');
  process.exit(1);
}
console.log('check:hex: sin colores hexadecimales fuera de src/styles/_tokens.scss e index.html.');
