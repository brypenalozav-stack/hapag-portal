// Verifica que cada <th> de cabecera de las plantillas tenga scope (guía UI/a11y/i18n §3.1, WCAG 1.3.1):
// dentro de cada <thead>…</thead>, toda etiqueta <th lleva el atributo scope=.
// Alcance: src/app (.html y plantillas en línea de los .ts). Termina con código 1 si encuentra alguno.
import { readdirSync, readFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = fileURLToPath(new URL('..', import.meta.url));
const APP = join(RAIZ, 'src', 'app');
const THEAD = /<thead\b[^>]*>[\s\S]*?<\/thead>/g;
const TH = /<th(?=[\s>/])[^>]*>/g;

function* archivos(dir) {
  for (const entrada of readdirSync(dir, { withFileTypes: true })) {
    const ruta = join(dir, entrada.name);
    if (entrada.isDirectory()) {
      yield* archivos(ruta);
    } else if (/\.(html|ts)$/.test(entrada.name) && !entrada.name.endsWith('.spec.ts')) {
      yield ruta;
    }
  }
}

function linea(texto, indice) {
  return texto.slice(0, indice).split('\n').length;
}

const hallazgos = [];
let cabeceras = 0;
for (const ruta of archivos(APP)) {
  const rel = relative(RAIZ, ruta).split(sep).join('/');
  const contenido = readFileSync(ruta, 'utf-8').replace(/\r\n/g, '\n');
  for (const thead of contenido.matchAll(THEAD)) {
    for (const th of thead[0].matchAll(TH)) {
      cabeceras++;
      if (!/\sscope\s*=/.test(th[0])) {
        hallazgos.push(`${rel}:${linea(contenido, thead.index + th.index)}: ${th[0]}`);
      }
    }
  }
}

if (hallazgos.length > 0) {
  console.error(`check:table-scope: ${hallazgos.length} <th> de cabecera sin scope:`);
  for (const h of hallazgos) console.error(`  ${h}`);
  console.error('Agregue scope="col" (o scope="row" en encabezados de fila).');
  process.exit(1);
}
console.log(`check:table-scope: ${cabeceras} <th> de cabecera revisados, todos con scope.`);
