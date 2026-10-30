// Verifica la calidad del portugués de Brasil de public/i18n/pt.json (guía UI/a11y/i18n §5.7):
//  (a) ninguna hoja igual al español (si es ≠ en), salvo la lista blanca igualEs;
//  (b) ninguna hoja igual al inglés (si es ≠ en), salvo la lista blanca igualEn;
//  (c) los términos del glosario (docs/requerimientos/glosario-es-en.md) presentes en es se traducen como dice el glosario;
//  (d) sin pt-PT, restos de español ni calcos; "por favor" como mucho 10 veces y nunca en claves de acción;
//  (e) sin tuteo (tu/teu/tua/ti/contigo), salvo la lista blanca tuteo;
//  (f) registro de autorrevisión docs/requerimientos/revision-pt-br.md con L1 a L8 y common/auth/errores/acciones al 100 %.
// Además: mismas claves y orden que es.json, parámetros iguales, ICU válido con =0 en todo plural, % pegado al número,
// UTF-8 sin BOM con CRLF y sangría de 2 espacios.
// Modos:
//  --secciones a,b            limita las comprobaciones a esas secciones (admite admin.{a,b}); ayuda local, CI no la usa.
//  --solo-glosario            solo la regla (c).
//  --solo-glosario --informe  calibración: coincidencias por fila del glosario y excepciones que harían falta
//                             (con pt.json si existe; si no, estimadas con en.json).
//  --fragmento <ruta.json>    valida un JSON plano {clave: valor pt} (subconjunto de es.json) con (a)-(e), parámetros e ICU,
//                             sin exigir el resto de las claves, el orden ni (f). Lo usan los lotes de traducción.
// Termina con código 1 si hay errores.
import { existsSync, readFileSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import MessageFormat from '@messageformat/core';

const RAIZ = fileURLToPath(new URL('..', import.meta.url));
const I18N = join(RAIZ, 'public', 'i18n');
const GLOSARIO = join(RAIZ, '..', 'docs', 'requerimientos', 'glosario-es-en.md');
const REVISION = join(RAIZ, '..', 'docs', 'requerimientos', 'revision-pt-br.md');
const IGUALES = join(RAIZ, 'scripts', 'i18n-pt-iguales.json');
const EXCEPCIONES = join(RAIZ, 'scripts', 'i18n-pt-glosario-excepciones.json');

const errores = [];
const avisos = [];

function salir(mensaje) {
  console.error(`check:i18n-pt: ${mensaje}`);
  process.exit(1);
}

// ---------------------------------------------------------------- Argumentos

/** "admin.{a, b},common" → ["admin.a", "admin.b", "common"]. */
function expandirSecciones(texto) {
  const partes = [];
  let nivel = 0;
  let actual = '';
  for (const c of texto) {
    if (c === '{') nivel++;
    if (c === '}') nivel--;
    if (c === ',' && nivel === 0) {
      partes.push(actual);
      actual = '';
    } else {
      actual += c;
    }
  }
  partes.push(actual);
  return partes
    .flatMap((p) => {
      const m = /^([^{]*)\{([^}]*)\}(.*)$/.exec(p.trim());
      return m ? m[2].split(',').map((s) => `${m[1]}${s.trim()}${m[3]}`) : [p.trim()];
    })
    .filter(Boolean);
}

const opciones = { secciones: null, soloGlosario: false, informe: false, fragmento: null };
const argumentos = process.argv.slice(2);
for (let i = 0; i < argumentos.length; i++) {
  const arg = argumentos[i];
  if (arg === '--secciones') {
    opciones.secciones = expandirSecciones(argumentos[++i] ?? '');
    if (opciones.secciones.length === 0) salir('--secciones necesita una lista, por ejemplo --secciones common,auth');
  } else if (arg === '--solo-glosario') {
    opciones.soloGlosario = true;
  } else if (arg === '--informe') {
    opciones.informe = true;
  } else if (arg === '--fragmento') {
    opciones.fragmento = argumentos[++i];
    if (!opciones.fragmento) salir('--fragmento necesita la ruta de un JSON plano {clave: valor}.');
  } else {
    salir(
      `argumento desconocido "${arg}". Uso: [--secciones a,b] [--solo-glosario [--informe]] [--fragmento <ruta.json>]`,
    );
  }
}
if (opciones.informe && !opciones.soloGlosario) salir('--informe solo se usa junto con --solo-glosario.');
if (opciones.fragmento && (opciones.secciones || opciones.soloGlosario)) {
  salir('--fragmento no se combina con --secciones ni con --solo-glosario.');
}

// ---------------------------------------------------------------- Utilidades

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

function leerTexto(ruta) {
  const bytes = readFileSync(ruta);
  try {
    return new TextDecoder('utf-8', { fatal: true }).decode(bytes);
  } catch {
    salir(`${relative(RAIZ, ruta)} no es UTF-8 válido.`);
  }
}

function leerJson(ruta) {
  if (!existsSync(ruta)) salir(`no existe ${relative(RAIZ, ruta)}.`);
  try {
    return JSON.parse(leerTexto(ruta).replace(/^﻿/, ''));
  } catch (e) {
    salir(`no se pudo leer ${relative(RAIZ, ruta)}: ${e.message}`);
  }
}

const escapar = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

/** Nombres de parámetros: interpolación Transloco {{ x }} y argumentos ICU de primer nivel (igual que check-i18n.mjs). */
function parametros(valor) {
  const nombres = new Set();
  const sinTransloco = valor.replace(/\{\{\s*([\w.]+)\s*\}\}/g, (_, n) => {
    nombres.add(n);
    return '';
  });
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

/** Texto visible: sin {{ x }} ni la sintaxis ICU (nombres de argumentos, plural/select y selectores). */
function soloTexto(valor) {
  const sinTransloco = valor.replace(/\{\{[^}]*\}\}/g, (m) => ' '.repeat(m.length));
  let nivel = 0;
  let salida = '';
  for (const c of sinTransloco) {
    if (c === '{') {
      nivel++;
      salida += ' ';
    } else if (c === '}') {
      nivel--;
      salida += ' ';
    } else {
      salida += nivel % 2 === 1 ? ' ' : c;
    }
  }
  return salida;
}

/** Argumentos plural sin =0 (en pt, "one" cubre 0 y 1). */
function pluralesSinCero(valor) {
  const texto = valor.replace(/\{\{[^}]*\}\}/g, 'x');
  const faltan = [];
  for (const m of texto.matchAll(/\{\s*(\w+)\s*,\s*plural\s*,/gu)) {
    let nivel = 0;
    let selector = '';
    let tieneCero = false;
    for (let i = m.index; i < texto.length; i++) {
      const c = texto[i];
      if (c === '{') {
        if (nivel === 1) {
          if (/(^|[\s,])=0\s*$/u.test(selector)) tieneCero = true;
          selector = '';
        }
        nivel++;
      } else if (c === '}') {
        nivel--;
        if (nivel === 0) break;
      } else if (nivel === 1) {
        selector += c;
      }
    }
    if (!tieneCero) faltan.push(m[1]);
  }
  return faltan;
}

/** Quita tildes y diacríticos letra a letra (para que "exenciones" coincida con "exención" + "es"). */
const sinTildes = (s) =>
  s.normalize('NFC').replace(/\p{L}/gu, (c) => {
    const base = c.normalize('NFD').replace(/\p{M}/gu, '');
    return base.length === 1 ? base : c;
  });

const ACCIONES =
  /^(submit|save|cancel|confirm|retry|close|back|next|delete|remove|edit|add|create|publish|unpublish|export|download|upload|search|apply|clear|pay)$/;
const esClaveDeAccion = (clave) => {
  const segmentos = clave.split('.');
  return ACCIONES.test(segmentos.at(-1)) || segmentos.includes('actions');
};

// ---------------------------------------------------------------- Datos

const es = aplanar(leerJson(join(I18N, 'es.json')));
const en = aplanar(leerJson(join(I18N, 'en.json')));

if (opciones.secciones) {
  for (const s of opciones.secciones) {
    if (![...es.keys()].some((k) => k === s || k.startsWith(`${s}.`)))
      errores.push(`--secciones: "${s}" no existe en es.json.`);
  }
}
const enSecciones = (clave) =>
  !opciones.secciones || opciones.secciones.some((s) => clave === s || clave.startsWith(`${s}.`));

function leerListaBlanca() {
  const lista = leerJson(IGUALES);
  for (const grupo of ['igualEs', 'igualEn', 'tuteo']) {
    const entradas = lista[grupo];
    if (!entradas || typeof entradas !== 'object' || Array.isArray(entradas)) {
      salir(`${relative(RAIZ, IGUALES)}: falta el objeto "${grupo}".`);
    }
    for (const [clave, motivo] of Object.entries(entradas)) {
      if (typeof motivo !== 'string' || motivo.trim() === '') {
        errores.push(`${relative(RAIZ, IGUALES)}: ${grupo}."${clave}" no tiene motivo.`);
      }
      if (!es.has(clave)) errores.push(`${relative(RAIZ, IGUALES)}: ${grupo}."${clave}" no existe en es.json.`);
    }
  }
  return lista;
}

// ---------------------------------------------------------------- Glosario (c)

/** "BL (conocimiento) / SWB" → ["BL", "SWB"]: sin paréntesis, alternativas separadas por " / ". */
const alternativas = (celda) =>
  celda
    .replace(/\([^)]*\)/g, '')
    .split(/\s+\/\s+/)
    .map((s) => s.trim().replace(/\s+/g, ' '))
    .filter(Boolean);

/** Palabra completa con plural opcional en cada palabra (s/es en es y en; s en pt) o prefijo si termina en "*". */
function regexTermino(alt, plural, flags) {
  const prefijo = alt.endsWith('*');
  const palabras = (prefijo ? alt.slice(0, -1) : alt).split(' ');
  const patron = palabras
    .map((p, i) => {
      if (prefijo && i === palabras.length - 1) return `${escapar(p)}\\p{L}*`;
      // Las palabras de una o dos letras ("de", "en") no llevan plural, salvo las siglas ("BL" → "BLs").
      const conPlural = /\p{L}$/u.test(p) && (p.length > 2 || /^\p{Lu}+$/u.test(p));
      return escapar(p) + (conPlural ? `(?:${plural})?` : '');
    })
    .join('\\s+');
  return new RegExp(`(?<!\\p{L})${patron}(?!\\p{L})`, flags);
}

function leerGlosario() {
  if (!existsSync(GLOSARIO)) salir(`no existe ${relative(RAIZ, GLOSARIO)}.`);
  const lineas = leerTexto(GLOSARIO)
    .split(/\r?\n/)
    .filter((l) => l.startsWith('|'));
  const celdas = (l) =>
    l
      .replace(/^\|/, '')
      .replace(/\|\s*$/, '')
      .split('|')
      .map((c) => c.trim());
  const cabecera = celdas(lineas[0] ?? '');
  if (cabecera.join('|') !== 'Español|English|Português|Nota de uso') {
    salir(
      `${relative(RAIZ, GLOSARIO)}: la tabla debe tener las columnas | Español | English | Português | Nota de uso |.`,
    );
  }
  return lineas.slice(2).map((l, i) => {
    const [celdaEs, celdaEn, celdaPt] = celdas(l);
    if (!celdaEs || !celdaEn || !celdaPt)
      salir(`${relative(RAIZ, GLOSARIO)}: la fila ${i + 1} no tiene las columnas ES, EN y PT.`);
    const altPt = alternativas(celdaPt);
    // Para estimar con en.json (informe sin pt.json) también cuenta lo que va entre paréntesis: "Carrier (haulier)".
    const altEn = [
      ...alternativas(celdaEn),
      ...[...celdaEn.matchAll(/\(([^)]*)\)/g)].flatMap((m) => alternativas(m[1])),
    ];
    return {
      es: celdaEs,
      pt: celdaPt,
      en: celdaEn,
      altPt,
      altEn,
      rePt: altPt.map((a) => regexTermino(a, 's', 'iu')),
      reEn: altEn.map((a) => regexTermino(a, 's|es', 'iu')),
      terminosEs: alternativas(celdaEs).map((a) => ({ alt: a, re: regexTermino(sinTildes(a), 's|es', 'giu') })),
    };
  });
}

const glosario = leerGlosario();
const excepcionesGlosario = leerJson(EXCEPCIONES);
for (const grupo of ['terminosSinChequeo', 'claves']) {
  const entradas = excepcionesGlosario[grupo];
  if (!entradas || typeof entradas !== 'object' || Array.isArray(entradas)) {
    salir(`${relative(RAIZ, EXCEPCIONES)}: falta el objeto "${grupo}".`);
  }
  for (const [entrada, motivo] of Object.entries(entradas)) {
    if (typeof motivo !== 'string' || motivo.trim() === '') {
      errores.push(`${relative(RAIZ, EXCEPCIONES)}: ${grupo}."${entrada}" no tiene motivo.`);
    }
  }
}
for (const termino of Object.keys(excepcionesGlosario.terminosSinChequeo)) {
  if (!glosario.some((f) => f.es === termino)) {
    errores.push(
      `${relative(RAIZ, EXCEPCIONES)}: terminosSinChequeo."${termino}" no es una fila (columna Español) del glosario.`,
    );
  }
}
for (const entrada of Object.keys(excepcionesGlosario.claves)) {
  const [clave, termino] = entrada.split('|');
  if (!termino || !glosario.some((f) => f.es === termino || f.terminosEs.some((t) => t.alt === termino))) {
    errores.push(`${relative(RAIZ, EXCEPCIONES)}: claves."${entrada}" debe ser "<clave>|<término ES del glosario>".`);
  } else if (!es.has(clave)) {
    errores.push(`${relative(RAIZ, EXCEPCIONES)}: claves."${entrada}": la clave no existe en es.json.`);
  }
}
const sinChequeo = (fila) => Object.hasOwn(excepcionesGlosario.terminosSinChequeo, fila.es);
const exceptuada = (clave, fila, alt) =>
  Object.hasOwn(excepcionesGlosario.claves, `${clave}|${fila.es}`) ||
  Object.hasOwn(excepcionesGlosario.claves, `${clave}|${alt}`);

// Coincidencia más larga primero: un tramo ya cubierto no se vuelve a evaluar con alternativas más cortas.
const terminosOrdenados = glosario
  .flatMap((fila) => fila.terminosEs.map((t) => ({ fila, ...t })))
  .sort((a, b) => b.alt.length - a.alt.length);

/** Filas del glosario presentes en el valor es (Map fila → alternativa ES encontrada). */
const cacheFilas = new Map();
function filasEnEspanol(valorEs) {
  if (cacheFilas.has(valorEs)) return cacheFilas.get(valorEs);
  const texto = sinTildes(soloTexto(valorEs));
  const cubierto = new Uint8Array(texto.length);
  const filas = new Map();
  for (const t of terminosOrdenados) {
    for (const m of texto.matchAll(t.re)) {
      const fin = m.index + m[0].length;
      if (cubierto.subarray(m.index, fin).some(Boolean)) continue;
      cubierto.fill(1, m.index, fin);
      if (!filas.has(t.fila)) filas.set(t.fila, t.alt);
    }
  }
  cacheFilas.set(valorEs, filas);
  return filas;
}

/** Términos del glosario que el valor traducido no respeta: [{ fila, alt }]. */
function faltasDeGlosario(clave, valorEs, valorTraducido, regexes) {
  const texto = soloTexto(valorTraducido);
  const faltas = [];
  for (const [fila, alt] of filasEnEspanol(valorEs)) {
    if (sinChequeo(fila) || exceptuada(clave, fila, alt)) continue;
    if (!fila[regexes].some((re) => re.test(texto))) faltas.push({ fila, alt });
  }
  return faltas;
}

// ---------------------------------------------------------------- Reglas por hoja

const PT_PT =
  'ecrã|telemóvel|registo|registar|utilizador|utilizadores|ficheiro|ficheiros|palavra-passe|contentor|equipa|facto';
const ESPANOL =
  'usted|ingrese|seleccione|también|después|ahora|aquí|cuenta|envío|correo|contraseña|carro|boleto|buque|nave|oficina';
const RE_CARACTERES = /[ñÑ¿¡«»]/gu;
const RE_PT_PT = new RegExp(`(?<!\\p{L})(?:${PT_PT})s?(?!\\p{L})`, 'giu');
const RE_ESPANOL = new RegExp(`(?<!\\p{L})(?:${ESPANOL})s?(?!\\p{L})`, 'giu');
// Tuteo en minúsculas (y con mayúscula inicial salvo "Ti"), distinguiendo mayúsculas para no marcar "TI".
const RE_TUTEO = /(?<!\p{L})(tu|teu|tua|teus|tuas|ti|contigo|Tu|Teu|Tua|Teus|Tuas|Contigo)(?!\p{L})/gu;
const RE_POR_FAVOR = /(?<!\p{L})por favor(?!\p{L})/giu;
const RE_REALIZAR = /(?<!\p{L})realiz\p{L}*/giu;
const RE_CONTESTAR = /(?<!\p{L})contest\p{L}*/giu;
const RE_PORCENTAJE = /(\d|\}\})\s%/u;

const mf = new MessageFormat('pt');
const halladas = (re, texto) => [...new Set([...texto.matchAll(re)].map((m) => m[0]))];

/**
 * Reglas (a) a (e), parámetros, ICU, =0 y %. Devuelve cuántas veces aparece "por favor".
 * `origen` es el nombre del archivo en los mensajes.
 */
function revisarHoja(origen, clave, pt, lista, conteos) {
  const valorEs = es.get(clave);
  const valorEn = en.get(clave);
  if (typeof pt !== 'string') {
    errores.push(`${origen}: "${clave}" no es texto.`);
    return;
  }
  if (pt.trim() === '') {
    errores.push(`${origen}: "${clave}" está vacío.`);
    return;
  }
  if (typeof valorEs !== 'string') return;
  const texto = soloTexto(pt);

  // Parámetros e ICU.
  if (parametros(pt) !== parametros(valorEs)) {
    errores.push(`${origen}: "${clave}" usa parámetros [${parametros(pt)}] y es.json [${parametros(valorEs)}].`);
  }
  try {
    mf.compile(pt.replace(/\{\{\s*([\w.]+)\s*\}\}/g, '$1'));
  } catch (e) {
    errores.push(`${origen}: "${clave}" no es un mensaje ICU válido: ${e.message}`);
  }
  for (const nombre of pluralesSinCero(pt)) {
    errores.push(`${origen}: "${clave}": el plural de "${nombre}" no tiene =0 (en pt, "one" también cubre el 0).`);
  }
  if (RE_PORCENTAJE.test(pt)) errores.push(`${origen}: "${clave}": el % va pegado al número ("10%").`);

  // (a) y (b) Igualdades.
  if (valorEs !== valorEn) {
    if (pt === valorEs && !Object.hasOwn(lista.igualEs, clave)) {
      errores.push(
        `${origen}: "${clave}" es igual al español ("${pt}"). Tradúzcalo o agréguelo a igualEs con su motivo.`,
      );
    }
    if (pt === valorEn && !Object.hasOwn(lista.igualEn, clave)) {
      errores.push(
        `${origen}: "${clave}" es igual al inglés ("${pt}"). Tradúzcalo o agréguelo a igualEn con su motivo.`,
      );
    }
  }

  // (c) Glosario.
  for (const { fila, alt } of faltasDeGlosario(clave, valorEs, pt, 'rePt')) {
    errores.push(
      `${origen}: "${clave}": el es usa "${alt}" (glosario: ${fila.es}) y el pt no usa ninguna de: ${fila.altPt.join(' / ')}.`,
    );
  }

  // (d) pt-PT, restos de español y calcos. Las hojas iguales en los tres idiomas (marcas, autónimos) no se revisan.
  if (!(pt === valorEs && pt === valorEn)) {
    const caracteres = halladas(RE_CARACTERES, texto);
    if (caracteres.length) errores.push(`${origen}: "${clave}": caracteres del español: ${caracteres.join(' ')}.`);
    const ptPt = halladas(RE_PT_PT, texto);
    if (ptPt.length) errores.push(`${origen}: "${clave}": términos de pt-PT: ${ptPt.join(', ')}.`);
    const espanol = halladas(RE_ESPANOL, texto);
    if (espanol.length) errores.push(`${origen}: "${clave}": palabras del español o calcos: ${espanol.join(', ')}.`);
  }
  const porFavor = [...texto.matchAll(RE_POR_FAVOR)].length;
  conteos.porFavor += porFavor;
  if (porFavor > 0 && esClaveDeAccion(clave)) {
    errores.push(`${origen}: "${clave}": "por favor" no va en botones ni acciones.`);
  }
  if (RE_REALIZAR.test(texto)) conteos.realizar.push(clave);
  RE_REALIZAR.lastIndex = 0;
  if (RE_CONTESTAR.test(texto)) conteos.contestar.push(clave);
  RE_CONTESTAR.lastIndex = 0;

  // (e) Tuteo.
  const tuteo = halladas(RE_TUTEO, texto);
  if (tuteo.length && !Object.hasOwn(lista.tuteo, clave)) {
    errores.push(`${origen}: "${clave}": tuteo (${tuteo.join(', ')}); use "você".`);
  }
}

function informarConteos(conteos, total) {
  if (conteos.porFavor > 10) {
    errores.push(`"por favor" aparece ${conteos.porFavor} veces en ${total}; el máximo es 10.`);
  }
  if (conteos.realizar.length) {
    avisos.push(
      `"realizar" (calco frecuente; prefiera "efetuar" o el verbo específico) en ${conteos.realizar.length} clave(s): ${conteos.realizar.join(', ')}`,
    );
  }
  if (conteos.contestar.length) {
    avisos.push(
      `"contestar" en ${conteos.contestar.length} clave(s); revise que signifique impugnar y no responder: ${conteos.contestar.join(', ')}`,
    );
  }
}

// ---------------------------------------------------------------- (f) Registro de autorrevisión

function revisarRegistro(completo) {
  const nombre = relative(RAIZ, REVISION);
  if (!existsSync(REVISION)) {
    errores.push(`(f) no existe ${nombre} (registro de autorrevisión).`);
    return;
  }
  const lineas = leerTexto(REVISION)
    .split(/\r?\n/)
    .filter((l) => l.startsWith('|'));
  const celdas = (l) =>
    l
      .replace(/^\|/, '')
      .replace(/\|\s*$/, '')
      .split('|')
      .map((c) => c.trim().replace(/`/g, ''));
  if (celdas(lineas[0] ?? '').join('|') !== 'Lote|Alcance|Fecha|Criterio|Resultado') {
    errores.push(`(f) ${nombre}: falta la tabla | Lote | Alcance | Fecha | Criterio | Resultado |.`);
    return;
  }
  const filas = lineas.slice(2).map(celdas);
  filas.forEach(([lote, alcance, fecha], i) => {
    const valida = /^\d{4}-\d{2}-\d{2}$/.test(fecha ?? '') && !Number.isNaN(Date.parse(`${fecha}T00:00:00Z`));
    if (!valida) errores.push(`(f) ${nombre}: la fila ${i + 1} (${lote} / ${alcance}) no tiene fecha AAAA-MM-DD.`);
  });
  if (!completo) return;
  for (const lote of ['L1', 'L2', 'L3', 'L4', 'L5', 'L6', 'L7', 'L8']) {
    if (!filas.some((f) => f[0] === lote)) errores.push(`(f) ${nombre}: falta la fila del lote ${lote}.`);
  }
  for (const alcance of ['common', 'auth', '*.errors.*', 'acciones']) {
    const deAlcance = filas.filter((f) => f[1] === alcance);
    if (deAlcance.length === 0) {
      errores.push(`(f) ${nombre}: falta la fila con alcance "${alcance}".`);
    } else if (!deAlcance.some((f) => /^revisado 100 %/.test(f[4] ?? ''))) {
      errores.push(`(f) ${nombre}: la fila con alcance "${alcance}" debe tener resultado "revisado 100 %".`);
    }
  }
}

// ---------------------------------------------------------------- Modo informe (calibración L0)

function informe(pt) {
  const claves = [...es.keys()].filter(enSecciones);
  const usaPt = pt !== null;
  console.log(
    `check:i18n-pt: calibración del glosario sobre ${claves.length} claves de es.json. ` +
      (usaPt
        ? 'Las excepciones se cuentan contra pt.json.'
        : 'pt.json no existe: las excepciones se estiman contra en.json (el en no usa ninguna alternativa EN de la fila).'),
  );
  console.log('');
  console.log('| Término ES (glosario) | Claves con el término | Excepciones necesarias | Estado |');
  console.log('|---|---:|---:|---|');
  let marcadas = 0;
  const detalle = [];
  for (const fila of glosario) {
    let coincidencias = 0;
    let faltas = 0;
    const clavesConFalta = [];
    for (const clave of claves) {
      const valorEs = es.get(clave);
      if (typeof valorEs !== 'string') continue;
      const encontradas = filasEnEspanol(valorEs);
      if (!encontradas.has(fila)) continue;
      coincidencias++;
      if (sinChequeo(fila) || exceptuada(clave, fila, encontradas.get(fila))) continue;
      const destino = usaPt ? pt.get(clave) : en.get(clave);
      const regexes = usaPt ? fila.rePt : fila.reEn;
      if (typeof destino !== 'string' || !regexes.some((re) => re.test(soloTexto(destino)))) {
        faltas++;
        clavesConFalta.push(clave);
      }
    }
    let estado = 'ok';
    if (sinChequeo(fila)) {
      estado = 'sin chequeo';
    } else if (coincidencias === 0) {
      estado = 'REFORMULAR: 0 coincidencias';
    } else if (faltas > 10) {
      estado = 'REFORMULAR: más de 10 excepciones';
    }
    if (estado.startsWith('REFORMULAR')) {
      marcadas++;
      if (clavesConFalta.length) detalle.push(`${fila.es}: ${clavesConFalta.join(', ')}`);
    }
    console.log(`| ${fila.es} | ${coincidencias} | ${sinChequeo(fila) ? '-' : faltas} | ${estado} |`);
  }
  console.log('');
  if (detalle.length) {
    console.log('Claves que necesitarían excepción en las filas por reformular:');
    for (const d of detalle) console.log(`  ${d}`);
    console.log('');
  }
  if (errores.length > 0) {
    console.error(`check:i18n-pt: ${errores.length} error(es):`);
    for (const e of errores) console.error(`  ${e}`);
    process.exit(1);
  }
  if (marcadas > 0) {
    console.error(`check:i18n-pt: ${marcadas} fila(s) del glosario por reformular antes de traducir.`);
    process.exit(1);
  }
  console.log(`check:i18n-pt: ${glosario.length} filas del glosario calibradas, ninguna por reformular.`);
  process.exit(0);
}

// ---------------------------------------------------------------- Modo fragmento

function fragmento(ruta) {
  const absoluta = resolve(process.cwd(), ruta);
  if (!existsSync(absoluta)) salir(`no existe el fragmento ${ruta}.`);
  const texto = leerTexto(absoluta);
  let datos;
  try {
    datos = JSON.parse(texto.replace(/^﻿/, ''));
  } catch (e) {
    salir(`el fragmento ${ruta} no es JSON válido: ${e.message}`);
  }
  if (datos === null || typeof datos !== 'object' || Array.isArray(datos)) {
    salir(`el fragmento ${ruta} debe ser un objeto plano {"clave.completa": "valor pt"}.`);
  }
  // JSON.parse se queda con la última clave repetida sin avisar: se buscan a mano (una clave por línea).
  const vistas = new Set();
  for (const linea of texto.split(/\r?\n/)) {
    const m = /^\s*"((?:[^"\\]|\\.)*)"\s*:/.exec(linea);
    if (!m) continue;
    if (vistas.has(m[1])) errores.push(`${ruta}: "${m[1]}" está repetida.`);
    vistas.add(m[1]);
  }
  const lista = leerListaBlanca();
  const conteos = { porFavor: 0, realizar: [], contestar: [] };
  let revisadas = 0;
  for (const [clave, valor] of Object.entries(datos)) {
    if (!es.has(clave)) {
      errores.push(`${ruta}: "${clave}" no existe en es.json (use la clave completa con puntos).`);
      continue;
    }
    revisarHoja(ruta, clave, valor, lista, conteos);
    revisadas++;
  }
  // El límite de 10 "por favor" es de todo pt.json: en un fragmento solo se informa.
  if (conteos.porFavor > 0)
    avisos.push(`"por favor" aparece ${conteos.porFavor} vez/veces en el fragmento (máximo 10 en todo pt.json).`);
  conteos.porFavor = 0;
  informarConteos(conteos, ruta);
  terminar(`${revisadas} claves del fragmento ${ruta} sin errores (a)-(e), parámetros ni ICU.`);
}

// ---------------------------------------------------------------- Salida

function terminar(resumen) {
  for (const a of avisos) console.warn(`check:i18n-pt: aviso, ${a}`);
  if (errores.length > 0) {
    console.error(`check:i18n-pt: ${errores.length} error(es):`);
    for (const e of errores) console.error(`  ${e}`);
    process.exit(1);
  }
  console.log(`check:i18n-pt: ${resumen}`);
}

// ---------------------------------------------------------------- Ejecución

if (opciones.fragmento) {
  fragmento(opciones.fragmento);
  process.exit(0);
}

const RUTA_PT = join(I18N, 'pt.json');
if (!existsSync(RUTA_PT)) {
  if (opciones.informe) informe(null);
  salir(
    `no existe ${relative(RAIZ, RUTA_PT)}. Ensámblelo desde los fragmentos con "node scripts/build-pt.mjs <directorio>" ` +
      'o valide un lote con --fragmento <ruta.json>.',
  );
}

const crudo = readFileSync(RUTA_PT);
let textoPt;
try {
  textoPt = new TextDecoder('utf-8', { fatal: true }).decode(crudo);
} catch {
  salir(`${relative(RAIZ, RUTA_PT)} no es UTF-8 válido.`);
}
let objetoPt;
try {
  objetoPt = JSON.parse(textoPt.replace(/^﻿/, ''));
} catch (e) {
  salir(`no se pudo leer ${relative(RAIZ, RUTA_PT)}: ${e.message}`);
}
const pt = aplanar(objetoPt);

if (opciones.informe) informe(pt);

// Formato del archivo: el mismo que es.json (UTF-8 sin BOM, CRLF, sangría de 2 y salto final).
// TextDecoder descarta el BOM, por eso se miran los bytes.
if (crudo[0] === 0xef && crudo[1] === 0xbb && crudo[2] === 0xbf)
  errores.push('pt.json: tiene BOM; debe ser UTF-8 sin BOM.');
if (/(^|[^\r])\n/.test(textoPt)) errores.push('pt.json: tiene saltos de línea LF; deben ser CRLF.');
if (textoPt.replace(/^﻿/, '') !== `${JSON.stringify(objetoPt, null, 2).replace(/\n/g, '\r\n')}\r\n`) {
  errores.push('pt.json: el formato no es el de es.json (sangría de 2 espacios, CRLF y salto final).');
}

const clavesEs = [...es.keys()].filter(enSecciones);
const clavesPt = [...pt.keys()].filter(enSecciones);
const conjuntoPt = new Set(clavesPt);
const conjuntoEs = new Set(clavesEs);
for (const clave of clavesEs)
  if (!conjuntoPt.has(clave)) errores.push(`pt.json: falta "${clave}" (existe en es.json).`);
for (const clave of clavesPt) if (!conjuntoEs.has(clave)) errores.push(`pt.json: "${clave}" no existe en es.json.`);
const ordenEs = clavesEs.filter((k) => conjuntoPt.has(k));
const ordenPt = clavesPt.filter((k) => conjuntoEs.has(k));
const primeraDistinta = ordenEs.findIndex((k, i) => ordenPt[i] !== k);
if (primeraDistinta >= 0) {
  errores.push(
    `pt.json: el orden de las claves difiere del de es.json desde "${ordenEs[primeraDistinta]}" ` +
      `(pt.json tiene "${ordenPt[primeraDistinta]}"); ensámblelo con scripts/build-pt.mjs.`,
  );
}

if (opciones.soloGlosario) {
  for (const clave of ordenEs) {
    const valorEs = es.get(clave);
    const valorPt = pt.get(clave);
    if (typeof valorEs !== 'string' || typeof valorPt !== 'string') continue;
    for (const { fila, alt } of faltasDeGlosario(clave, valorEs, valorPt, 'rePt')) {
      errores.push(
        `pt.json: "${clave}": el es usa "${alt}" (glosario: ${fila.es}) y el pt no usa ninguna de: ${fila.altPt.join(' / ')}.`,
      );
    }
  }
  terminar(`${ordenEs.length} claves respetan el glosario (${glosario.length} filas).`);
  process.exit(0);
}

const lista = leerListaBlanca();
const conteos = { porFavor: 0, realizar: [], contestar: [] };
for (const clave of ordenEs) revisarHoja('pt.json', clave, pt.get(clave), lista, conteos);
informarConteos(conteos, opciones.secciones ? 'las secciones revisadas' : 'pt.json');

// Entradas de las listas blancas que ya no hacen falta.
for (const clave of Object.keys(lista.igualEs)) {
  if (enSecciones(clave) && pt.has(clave) && pt.get(clave) !== es.get(clave)) {
    avisos.push(`igualEs."${clave}" sobra: el pt ya no es igual al español.`);
  }
}
for (const clave of Object.keys(lista.igualEn)) {
  if (enSecciones(clave) && pt.has(clave) && pt.get(clave) !== en.get(clave)) {
    avisos.push(`igualEn."${clave}" sobra: el pt ya no es igual al inglés.`);
  }
}

revisarRegistro(!opciones.secciones);

terminar(
  `${ordenEs.length} claves${opciones.secciones ? ` de ${opciones.secciones.join(', ')}` : ''} en pt-BR sin errores ` +
    '(igualdades, glosario, pt-PT, español, tuteo, =0, %, parámetros, orden, formato y autorrevisión).',
);
