// Servidor estático de las pruebas de Playwright (sin dependencias): sirve la compilación `e2e` de la aplicación
// (dist/e2e/browser) con respaldo SPA a index.html, como el nginx de producción (nginx.conf: try_files … /index.html).
// Reemplaza a `ng serve` (el servidor de desarrollo recompilaba y recargaba la página) como webServer de
// playwright.config.ts: Playwright espera a que responda antes de empezar y sirve para revisar la compilación a mano.
// Durante las pruebas el navegador recibe estos mismos archivos desde memoria (e2e/fixtures/app.ts), sin conexiones TCP.
//
// - Los archivos se leen una sola vez al arrancar y se sirven desde memoria (sin lecturas de disco por solicitud).
// - index.html se sirve sin las hojas de Google Fonts: son externas y bloquean el evento load (una respuesta lenta de
//   fonts.googleapis.com dejaba page.goto esperando); la interfaz usa las fuentes de respaldo.
//
// Uso: node e2e/serve-static.mjs [puerto]   (por defecto 4300; también E2E_PORT).
// Las llamadas a /api/v1/** las responde page.route en cada prueba (e2e/fixtures/api-mocks.ts).
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { createServer } from 'node:http';
import { extname, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = resolve(fileURLToPath(new URL('..', import.meta.url)), 'dist', 'e2e', 'browser');
const PUERTO = Number(process.argv[2] ?? process.env['E2E_PORT'] ?? 4300);
/** Enlaces a Google Fonts (preconnect y hoja de estilos) que se quitan de index.html. */
const FUENTES_EXTERNAS = /<link\b[^>]*https:\/\/fonts\.(?:googleapis|gstatic)\.com[^>]*>\s*/g;

const TIPOS = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.map': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif',
  '.ico': 'image/x-icon',
  '.webp': 'image/webp',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.ttf': 'font/ttf',
  '.txt': 'text/plain; charset=utf-8',
  '.pdf': 'application/pdf',
};

if (!existsSync(join(RAIZ, 'index.html'))) {
  console.error(`serve-static: no existe ${join(RAIZ, 'index.html')}. Compile antes con: npx ng build --configuration e2e`);
  process.exit(1);
}

/** Archivos de la compilación por ruta URL (`/main-XXXX.js`), con su tipo y contenido. */
const ARCHIVOS = new Map();

function cargar(dir) {
  for (const entrada of readdirSync(dir, { withFileTypes: true })) {
    const ruta = join(dir, entrada.name);
    if (entrada.isDirectory()) {
      cargar(ruta);
      continue;
    }
    const url = '/' + relative(RAIZ, ruta).split(sep).join('/');
    let contenido = readFileSync(ruta);
    if (url === '/index.html') contenido = Buffer.from(contenido.toString('utf-8').replace(FUENTES_EXTERNAS, ''), 'utf-8');
    ARCHIVOS.set(url, { tipo: TIPOS[extname(ruta).toLowerCase()] ?? 'application/octet-stream', contenido });
  }
}
cargar(RAIZ);
const INDICE = ARCHIVOS.get('/index.html');

function enviar(res, archivo, metodo, estado = 200) {
  res.writeHead(estado, {
    'Content-Type': archivo.tipo,
    'Content-Length': archivo.contenido.length,
    'Cache-Control': 'no-store',
  });
  res.end(metodo === 'HEAD' ? undefined : archivo.contenido);
}

const servidor = createServer((req, res) => {
  const metodo = req.method ?? 'GET';
  if (metodo !== 'GET' && metodo !== 'HEAD') {
    res.writeHead(405, { Allow: 'GET, HEAD' });
    res.end();
    return;
  }
  let pathname;
  try {
    pathname = decodeURIComponent(new URL(req.url ?? '/', 'http://localhost').pathname);
  } catch {
    pathname = '/';
  }
  const archivo = ARCHIVOS.get(pathname);
  if (archivo) {
    enviar(res, archivo, metodo);
    return;
  }
  // Un recurso estático conocido (.js, .css, .json…) que no existe es un 404 real; el resto son rutas de la SPA
  // (respaldo a index.html), aunque un parámetro de la ruta lleve un punto.
  if (TIPOS[extname(pathname).toLowerCase()]) {
    enviar(res, { tipo: TIPOS['.txt'], contenido: Buffer.from('Not found') }, metodo, 404);
    return;
  }
  enviar(res, INDICE, metodo);
});

servidor.keepAliveTimeout = 65_000;
servidor.listen(PUERTO, () => {
  console.log(`serve-static: ${ARCHIVOS.size} archivos de ${RAIZ} en http://localhost:${PUERTO}`);
});

for (const senal of ['SIGINT', 'SIGTERM']) {
  process.on(senal, () => servidor.close(() => process.exit(0)));
}
