// Verifica que no queden textos visibles escritos a mano en las plantillas (guía UI/a11y/i18n §5.6):
//  - nodos de texto con letras, fuera de {{ … }} y de los bloques de control (@if, @for, …);
//  - atributos accesibles con letras: placeholder, title, alt, aria-label, aria-description,
//    aria-placeholder, aria-roledescription, aria-valuetext y label, estáticos o enlazados
//    ([placeholder]="'Texto'", [attr.aria-label]="…");
//  - literales con letras dentro de {{ … }} que no sean claves (salvo operandos de comparaciones);
//  - en el TypeScript de los componentes, mensajes literales pasados a .set('…') o en propiedades label: '…'.
// Alcance: src/app/app.html, src/app/features/** y src/app/shared/components/** (.html y plantillas
// en línea `template:` de los .ts). Termina con código 1 si encuentra alguno.
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = fileURLToPath(new URL('..', import.meta.url));
const ALCANCE = ['src/app/app.html', 'src/app/features', 'src/app/shared/components'];

/**
 * Excepciones: textos que se muestran igual en todos los idiomas y no se traducen.
 * - Marca: "Hapag-Lloyd" y las iniciales "HL" del logo.
 * - Códigos ISO del selector de idioma (ES, EN), de país (CL, BO) y de moneda (USD, CLP, BOB, EUR).
 */
const EXCEPCIONES = ['Hapag-Lloyd', 'HL', 'ES', 'EN', 'CL', 'BO', 'USD', 'CLP', 'BOB', 'EUR'];

const ATRIBUTOS_ACCESIBLES = new Set([
  'placeholder',
  'title',
  'alt',
  'label',
  'aria-label',
  'aria-description',
  'aria-placeholder',
  'aria-roledescription',
  'aria-valuetext',
]);

const FORMA_CLAVE = /^[a-z][A-Za-z0-9]*(\.[A-Za-z0-9_-]+)+$/;
const LETRA = /\p{L}/u;
const BLOQUES = /^@(else\s+if|if|else|for|switch|case|default|defer|placeholder|loading|error|empty|let)\b/;

const hallazgos = [];

function quitarExcepciones(texto) {
  let t = texto.replace(/&[a-zA-Z]+;|&#\d+;/g, ' ');
  for (const e of EXCEPCIONES) {
    // Las excepciones solo tienen letras y guiones: no hace falta escaparlas en la expresión.
    t = t.replace(new RegExp(`(^|[^\\p{L}\\d-])${e}(?![\\p{L}\\d-])`, 'gu'), '$1');
  }
  return t;
}

function tieneLetras(texto) {
  return LETRA.test(quitarExcepciones(texto));
}

/** Literales de una expresión Angular que se mostrarían como texto (no claves ni operandos de comparación). */
function literalesVisibles(expr) {
  const encontrados = [];
  const re = /'((?:[^'\\]|\\.)*)'|"((?:[^"\\]|\\.)*)"/g;
  for (const m of expr.matchAll(re)) {
    const literal = m[1] ?? m[2];
    const antes = expr.slice(0, m.index).trimEnd();
    const despues = expr.slice(m.index + m[0].length).trimStart();
    const comparacion = /(===|!==|==|!=)$/.test(antes) || /^(===|!==|==|!=)/.test(despues);
    // Argumento de pipe (`| hlDate:'datetime'`) o acceso por índice (`counts()['OnTrack']`): no se muestra.
    const argumentoPipe = /\|\s*[A-Za-z]\w*\s*:$/.test(antes);
    const indice = antes.endsWith('[') && despues.startsWith(']');
    if (comparacion || argumentoPipe || indice || FORMA_CLAVE.test(literal) || !tieneLetras(literal)) continue;
    encontrados.push(literal);
  }
  return encontrados;
}

function linea(texto, indice) {
  return texto.slice(0, indice).split('\n').length;
}

/** Quita interpolaciones y encabezados de bloques de control de un nodo de texto; devuelve lo que queda. */
function textoVisible(nodo, registrar) {
  let salida = '';
  let i = 0;
  while (i < nodo.length) {
    if (nodo.startsWith('{{', i)) {
      const fin = nodo.indexOf('}}', i + 2);
      const expr = nodo.slice(i + 2, fin === -1 ? nodo.length : fin);
      for (const lit of literalesVisibles(expr)) registrar(i, `literal en interpolación: '${lit}'`);
      i = fin === -1 ? nodo.length : fin + 2;
      continue;
    }
    const m = nodo[i] === '@' ? BLOQUES.exec(nodo.slice(i)) : null;
    if (m) {
      i += m[0].length;
      while (/\s/.test(nodo[i] ?? '')) i++;
      if (m[1] === 'let') {
        const fin = nodo.indexOf(';', i);
        i = fin === -1 ? nodo.length : fin + 1;
        continue;
      }
      if (nodo[i] === '(') {
        let nivel = 0;
        let comilla = null;
        for (; i < nodo.length; i++) {
          const c = nodo[i];
          if (comilla) {
            if (c === comilla) comilla = null;
          } else if (c === "'" || c === '"') {
            comilla = c;
          } else if (c === '(') {
            nivel++;
          } else if (c === ')' && --nivel === 0) {
            i++;
            break;
          }
        }
      }
      continue;
    }
    salida += nodo[i] === '{' || nodo[i] === '}' ? ' ' : nodo[i];
    i++;
  }
  return salida;
}

/** Recorre una plantilla: nodos de texto y atributos de cada etiqueta. */
function revisarPlantilla(rel, plantilla, desplazamientoLinea) {
  const html = plantilla.replace(/<!--[\s\S]*?-->/g, (c) => c.replace(/[^\n]/g, ' '));
  const reportar = (indice, detalle) =>
    hallazgos.push(`${rel}:${desplazamientoLinea + linea(html, indice)}: ${detalle}`);

  let i = 0;
  let inicioTexto = 0;
  const cerrarTexto = (fin) => {
    const nodo = html.slice(inicioTexto, fin);
    const visible = textoVisible(nodo, (rel2, detalle) => reportar(inicioTexto + rel2, detalle));
    if (tieneLetras(visible)) {
      const muestra = visible.replace(/\s+/g, ' ').trim().slice(0, 80);
      const primera = nodo.search(/\S/);
      reportar(inicioTexto + Math.max(primera, 0), `texto: "${muestra}"`);
    }
  };

  while (i < html.length) {
    if (html[i] === '<' && /[A-Za-z/!]/.test(html[i + 1] ?? '')) {
      cerrarTexto(i);
      // Etiqueta: hasta el '>' que no esté entre comillas.
      let j = i + 1;
      let comilla = null;
      for (; j < html.length; j++) {
        const c = html[j];
        if (comilla) {
          if (c === comilla) comilla = null;
        } else if (c === '"' || c === "'") {
          comilla = c;
        } else if (c === '>') {
          break;
        }
      }
      const etiqueta = html.slice(i, j + 1);
      const reAttr = /([^\s=/>]+)\s*=\s*("([^"]*)"|'([^']*)')/g;
      for (const m of etiqueta.matchAll(reAttr)) {
        const nombre = m[1];
        const valor = m[3] ?? m[4] ?? '';
        const enlazado = /^\[(attr\.)?([^\]]+)\]$/.exec(nombre);
        const base = enlazado ? enlazado[2] : nombre.replace(/^attr\./, '');
        if (!ATRIBUTOS_ACCESIBLES.has(base)) continue;
        const indice = i + m.index;
        if (enlazado) {
          for (const lit of literalesVisibles(valor)) reportar(indice, `atributo ${nombre} con literal '${lit}'`);
        } else {
          const visible = textoVisible(valor, () => undefined);
          for (const lit of [...valor.matchAll(/\{\{([\s\S]*?)\}\}/g)].flatMap((x) => literalesVisibles(x[1]))) {
            reportar(indice, `atributo ${nombre} con literal '${lit}'`);
          }
          if (tieneLetras(visible)) reportar(indice, `atributo ${nombre}="${valor}"`);
        }
      }
      i = j + 1;
      inicioTexto = i;
      continue;
    }
    i++;
  }
  cerrarTexto(html.length);
}

/** Plantilla en línea (`template: \`…\``) de un componente y su línea de inicio. */
function plantillaEnLinea(codigo) {
  const m = /\btemplate\s*:\s*`/.exec(codigo);
  if (!m) return null;
  const inicio = m.index + m[0].length;
  const fin = codigo.indexOf('`', inicio);
  return { texto: codigo.slice(inicio, fin), inicio, fin, linea: linea(codigo, inicio) - 1 };
}

/** Mensajes literales en el TypeScript del componente (fuera de la plantilla). */
function revisarTs(rel, codigo, plantilla) {
  const sinPlantilla = plantilla
    ? codigo.slice(0, plantilla.inicio) + codigo.slice(plantilla.inicio, plantilla.fin).replace(/[^\n]/g, ' ') + codigo.slice(plantilla.fin)
    : codigo;
  const sinComentarios = sinPlantilla
    .replace(/\/\*[\s\S]*?\*\//g, (c) => c.replace(/[^\n]/g, ' '))
    .replace(/\/\/[^\n]*/g, (c) => c.replace(/./g, ' '));
  const patrones = [
    { re: /\.set\(\s*(['"`])((?:\\.|(?!\1)[^\\])*)\1/g, que: '.set()' },
    { re: /\blabel\s*:\s*(['"`])((?:\\.|(?!\1)[^\\])*)\1/g, que: 'label:' },
  ];
  for (const { re, que } of patrones) {
    for (const m of sinComentarios.matchAll(re)) {
      const literal = m[2];
      if (FORMA_CLAVE.test(literal) || !tieneLetras(literal.replace(/\$\{[^}]*\}/g, ''))) continue;
      hallazgos.push(`${rel}:${linea(sinComentarios, m.index)}: mensaje literal en ${que} '${literal}' (use translate())`);
    }
  }
}

function* archivos(ruta) {
  if (!existsSync(ruta)) return;
  if (statSync(ruta).isFile()) {
    yield ruta;
    return;
  }
  for (const entrada of readdirSync(ruta, { withFileTypes: true })) {
    const hijo = join(ruta, entrada.name);
    if (entrada.isDirectory()) yield* archivos(hijo);
    else if (/\.(html|ts)$/.test(entrada.name) && !entrada.name.endsWith('.spec.ts')) yield hijo;
  }
}

let revisados = 0;
for (const destino of ALCANCE) {
  for (const ruta of archivos(join(RAIZ, destino))) {
    const rel = relative(RAIZ, ruta).split(sep).join('/');
    const contenido = readFileSync(ruta, 'utf-8').replace(/\r\n/g, '\n');
    revisados++;
    if (ruta.endsWith('.html')) {
      revisarPlantilla(rel, contenido, 0);
    } else {
      const plantilla = plantillaEnLinea(contenido);
      if (plantilla) revisarPlantilla(rel, plantilla.texto, plantilla.linea);
      revisarTs(rel, contenido, plantilla);
    }
  }
}

if (hallazgos.length > 0) {
  console.error(`check:i18n-text: ${hallazgos.length} texto(s) sin traducir:`);
  for (const h of hallazgos) console.error(`  ${h}`);
  console.error('Mueva el texto a public/i18n/{es,en}.json y use el pipe transloco o translate().');
  process.exit(1);
}
console.log(`check:i18n-text: ${revisados} archivos revisados, sin textos ni atributos accesibles literales.`);
