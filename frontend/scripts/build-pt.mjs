// Ensambla public/i18n/pt.json desde los fragmentos de traducción de un directorio (plan pt-BR, Fase 2):
//  - cada *.json del directorio es un JSON plano {"clave.completa": "valor pt"} (lo valida check-i18n-pt.mjs --fragmento);
//  - pt.json queda con exactamente la estructura y el orden de es.json, en UTF-8 sin BOM, con el fin de línea de es.json y sangría de 2 espacios.
// Falla, sin escribir nada, si faltan claves de es.json, sobran claves, hay claves repetidas o valores que no son texto.
// Uso: node scripts/build-pt.mjs <directorio-de-fragmentos>. Ayuda local de los lotes: CI no la usa.
import { existsSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = fileURLToPath(new URL('..', import.meta.url));
const I18N = join(RAIZ, 'public', 'i18n');

function salir(mensaje) {
  console.error(`build-pt: ${mensaje}`);
  process.exit(1);
}

const directorio = process.argv[2];
if (!directorio || process.argv.length > 3) salir('uso: node scripts/build-pt.mjs <directorio-de-fragmentos>');
const DIR = resolve(process.cwd(), directorio);
if (!existsSync(DIR) || !statSync(DIR).isDirectory()) salir(`no existe el directorio ${directorio}.`);

function leerTexto(ruta) {
  try {
    return new TextDecoder('utf-8', { fatal: true }).decode(readFileSync(ruta)).replace(/^﻿/, '');
  } catch {
    salir(`${ruta} no es UTF-8 válido.`);
  }
}

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

const es = JSON.parse(leerTexto(join(I18N, 'es.json')));
const clavesEs = aplanar(es);

const errores = [];
const valores = new Map();
const origen = new Map();
const fragmentos = readdirSync(DIR)
  .filter((n) => n.toLowerCase().endsWith('.json'))
  .sort();
if (fragmentos.length === 0) salir(`${directorio} no tiene fragmentos .json.`);

for (const nombre of fragmentos) {
  const texto = leerTexto(join(DIR, nombre));
  let datos;
  try {
    datos = JSON.parse(texto);
  } catch (e) {
    errores.push(`${nombre}: no es JSON válido: ${e.message}`);
    continue;
  }
  if (datos === null || typeof datos !== 'object' || Array.isArray(datos)) {
    errores.push(`${nombre}: debe ser un objeto plano {"clave.completa": "valor pt"}.`);
    continue;
  }
  // JSON.parse se queda con la última clave repetida sin avisar: se buscan a mano (una clave por línea).
  const vistas = new Set();
  for (const linea of texto.split(/\r?\n/)) {
    const m = /^\s*"((?:[^"\\]|\\.)*)"\s*:/.exec(linea);
    if (!m) continue;
    if (vistas.has(m[1])) errores.push(`${nombre}: "${m[1]}" está repetida.`);
    vistas.add(m[1]);
  }
  for (const [clave, valor] of Object.entries(datos)) {
    if (typeof valor !== 'string') {
      errores.push(`${nombre}: "${clave}" no es texto (los fragmentos son planos).`);
    } else if (valores.has(clave)) {
      errores.push(`${nombre}: "${clave}" ya está en ${origen.get(clave)}.`);
    } else {
      valores.set(clave, valor);
      origen.set(clave, nombre);
    }
  }
}

const faltantes = [...clavesEs.keys()].filter((k) => !valores.has(k));
const sobrantes = [...valores.keys()].filter((k) => !clavesEs.has(k));
for (const clave of sobrantes) errores.push(`${origen.get(clave)}: "${clave}" no existe en es.json (sobra).`);
if (faltantes.length > 0) {
  errores.push(`faltan ${faltantes.length} clave(s) de es.json:`);
  for (const clave of faltantes) errores.push(`  ${clave}`);
}

if (errores.length > 0) {
  console.error(`build-pt: ${errores.length} error(es); no se escribió pt.json:`);
  for (const e of errores) console.error(`  ${e}`);
  process.exit(1);
}

/** Copia la estructura de es.json (mismo orden) con los valores pt. */
function construir(obj, prefijo = '') {
  const salida = {};
  for (const [k, v] of Object.entries(obj)) {
    const clave = prefijo ? `${prefijo}.${k}` : k;
    salida[k] = v !== null && typeof v === 'object' && !Array.isArray(v) ? construir(v, clave) : valores.get(clave);
  }
  return salida;
}

const destino = join(I18N, 'pt.json');
const eol = readFileSync(join(I18N, 'es.json'), 'utf-8').includes('\r\n') ? '\r\n' : '\n';
writeFileSync(destino, `${JSON.stringify(construir(es), null, 2).replace(/\n/g, eol)}${eol}`, 'utf-8');
console.log(
  `build-pt: ${relative(RAIZ, destino)} escrito con ${valores.size} claves de ${fragmentos.length} fragmento(s), en el orden de es.json.`,
);
