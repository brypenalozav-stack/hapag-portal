// Verifica los textos de public/i18n (guía UI/a11y/i18n §5.6):
//  - mismas claves en es.json y en.json;
//  - ningún valor vacío ni que no sea texto;
//  - mismos parámetros ({{ x }} y argumentos ICU) en ambos idiomas;
//  - mensajes ICU válidos (se compilan con @messageformat/core, como en tiempo de ejecución);
//  - ninguna clave usada en src/app (plantillas y TypeScript) que no exista.
// Las claves sin uso se informan como aviso. Termina con código 1 si hay errores.
import { readdirSync, readFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import MessageFormat from '@messageformat/core';

const RAIZ = fileURLToPath(new URL('..', import.meta.url));
const I18N = join(RAIZ, 'public', 'i18n');
const APP = join(RAIZ, 'src', 'app');
const IDIOMAS = ['es', 'en'];
const REFERENCIA = 'es';

const errores = [];

function aplanar(obj, prefijo = '', salida = new Map()) {
  for (const [k, v] of Object.entries(obj)) {
    const clave = prefijo ? `${prefijo}.${k}` : k;
    if (v !== null && typeof v === 'object' && !Array.isArray(v)) {
      aplanar(v, clave, salida);
    } else {
      salida.set(clave, v);
    }
  }
  return salida;
}

/** Nombres de parámetros: interpolación Transloco {{ x }} y argumentos ICU de primer nivel {x} / {x, plural, …}. */
function parametros(valor) {
  const nombres = new Set();
  const sinTransloco = valor.replace(/\{\{\s*([\w.]+)\s*\}\}/g, (_, n) => {
    nombres.add(n);
    return '';
  });
  // En ICU los niveles pares de llaves abren argumentos ({x, plural, …}) y los impares, submensajes ({# fila}).
  let nivel = 0;
  for (let i = 0; i < sinTransloco.length; i++) {
    const c = sinTransloco[i];
    if (c === '{') {
      const m = nivel % 2 === 0 ? /^\{\s*(\w+)\s*[,}]/.exec(sinTransloco.slice(i)) : null;
      if (m) nombres.add(m[1]);
      nivel++;
    } else if (c === '}') {
      nivel--;
    }
  }
  return [...nombres].sort().join(',');
}

// 1. Carga y aplanado.
const textos = {};
for (const idioma of IDIOMAS) {
  const ruta = join(I18N, `${idioma}.json`);
  try {
    textos[idioma] = aplanar(JSON.parse(readFileSync(ruta, 'utf-8')));
  } catch (e) {
    console.error(`check:i18n: no se pudo leer ${relative(RAIZ, ruta)}: ${e.message}`);
    process.exit(1);
  }
}

// 2. Paridad, valores vacíos, parámetros e ICU.
const referencia = textos[REFERENCIA];
for (const idioma of IDIOMAS) {
  const actual = textos[idioma];
  const mf = new MessageFormat(idioma);
  for (const [clave, valor] of actual) {
    if (typeof valor !== 'string') {
      errores.push(`${idioma}.json: "${clave}" no es texto.`);
      continue;
    }
    if (valor.trim() === '') {
      errores.push(`${idioma}.json: "${clave}" está vacío.`);
    }
    try {
      mf.compile(valor.replace(/\{\{\s*([\w.]+)\s*\}\}/g, '$1'));
    } catch (e) {
      errores.push(`${idioma}.json: "${clave}" no es un mensaje ICU válido: ${e.message}`);
    }
    if (idioma !== REFERENCIA) {
      if (!referencia.has(clave)) {
        errores.push(`${idioma}.json: "${clave}" no existe en ${REFERENCIA}.json.`);
      } else if (typeof referencia.get(clave) === 'string' && parametros(valor) !== parametros(referencia.get(clave))) {
        errores.push(
          `${idioma}.json: "${clave}" usa parámetros [${parametros(valor)}] y ${REFERENCIA}.json [${parametros(referencia.get(clave))}].`,
        );
      }
    }
  }
  if (idioma !== REFERENCIA) {
    for (const clave of referencia.keys()) {
      if (!actual.has(clave)) errores.push(`${idioma}.json: falta "${clave}" (existe en ${REFERENCIA}.json).`);
    }
  }
}

// 3. Claves usadas en src/app: literales con forma de clave cuyo primer segmento es un espacio de nombres de es.json.
const espacios = new Set([...referencia.keys()].map((k) => k.split('.')[0]));
const FORMA_CLAVE = /^[a-z][A-Za-z0-9]*(\.[A-Za-z0-9_-]+)+$/;
const LITERAL = /'((?:[^'\\\n]|\\.)*)'/g;

function* archivos(dir) {
  for (const entrada of readdirSync(dir, { withFileTypes: true })) {
    const ruta = join(dir, entrada.name);
    if (entrada.isDirectory()) yield* archivos(ruta);
    else if (/\.(ts|html)$/.test(entrada.name) && !entrada.name.endsWith('.spec.ts')) yield ruta;
  }
}

const usadas = new Set();
for (const ruta of archivos(APP)) {
  const rel = relative(RAIZ, ruta).split(sep).join('/');
  readFileSync(ruta, 'utf-8')
    .split(/\r?\n/)
    .forEach((linea, i) => {
      if (/^\s*import\s/.test(linea)) return;
      for (const m of linea.matchAll(LITERAL)) {
        const literal = m[1];
        if (!FORMA_CLAVE.test(literal) || !espacios.has(literal.split('.')[0])) continue;
        usadas.add(literal);
        if (!referencia.has(literal)) errores.push(`${rel}:${i + 1}: usa la clave "${literal}", que no existe.`);
      }
    });
}

const sinUso = [...referencia.keys()].filter((k) => !usadas.has(k));

if (errores.length > 0) {
  console.error(`check:i18n: ${errores.length} error(es):`);
  for (const e of errores) console.error(`  ${e}`);
  process.exit(1);
}
if (sinUso.length > 0) {
  console.warn(`check:i18n: aviso, ${sinUso.length} clave(s) sin uso literal en src/app:`);
  for (const k of sinUso) console.warn(`  ${k}`);
}
console.log(
  `check:i18n: ${referencia.size} claves con paridad en ${IDIOMAS.join('/')}, sin valores vacíos; ` +
    `${usadas.size} claves usadas en src/app, todas definidas.`,
);
