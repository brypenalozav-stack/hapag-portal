import { readFileSync, readdirSync, statSync } from 'fs';
import path from 'path';
import { test as base, expect } from '@playwright/test';

/**
 * Prueba de Playwright con la aplicación entregada desde memoria (fixture automática `aplicacion`).
 *
 * La compilación `e2e` (dist/e2e/browser, la misma que sirve e2e/serve-static.mjs) se lee una vez por worker y cada
 * archivo de la aplicación se responde con route.fulfill, con respaldo SPA a index.html como el nginx de producción.
 * Así el navegador no abre conexiones TCP a localhost: en Windows, Chromium dejaba de vez en cuando una conexión nueva
 * esperando ~19 s (o más) aunque el servidor respondía en milisegundos, y page.goto agotaba el tiempo de la prueba y
 * terminaba con net::ERR_ABORTED ("frame was detached"), con `ng serve` y con un servidor estático por igual.
 *
 * index.html se entrega sin las hojas de Google Fonts (externas, bloquean el evento load): la interfaz usa las fuentes
 * de respaldo y las pruebas no dependen de la red. Las rutas /api/** no se tocan: las responden los page.route de cada
 * prueba (api-mocks.ts), que tienen prioridad sobre las rutas del contexto.
 */

const RAIZ = path.resolve(__dirname, '..', '..', 'dist', 'e2e', 'browser');
const FUENTES_EXTERNAS = /<link\b[^>]*https:\/\/fonts\.(?:googleapis|gstatic)\.com[^>]*>\s*/g;

const TIPOS: Record<string, string> = {
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
};

interface Archivo {
  tipo: string;
  contenido: Buffer;
}

let archivos: Map<string, Archivo> | null = null;

/** Archivos de la compilación por ruta URL; se leen la primera vez que una prueba los necesita (ya compilados). */
function compilacion(): Map<string, Archivo> {
  if (archivos) return archivos;
  const leidos = new Map<string, Archivo>();
  const cargar = (dir: string): void => {
    for (const nombre of readdirSync(dir)) {
      const ruta = path.join(dir, nombre);
      if (statSync(ruta).isDirectory()) {
        cargar(ruta);
        continue;
      }
      const url = '/' + path.relative(RAIZ, ruta).split(path.sep).join('/');
      let contenido = readFileSync(ruta);
      if (url === '/index.html') contenido = Buffer.from(contenido.toString('utf-8').replace(FUENTES_EXTERNAS, ''), 'utf-8');
      leidos.set(url, { tipo: TIPOS[path.extname(ruta).toLowerCase()] ?? 'application/octet-stream', contenido });
    }
  };
  try {
    cargar(RAIZ);
  } catch (e) {
    throw new Error(`No se pudo leer ${RAIZ}: compile antes con "npx ng build --configuration e2e" (${(e as Error).message})`);
  }
  if (!leidos.has('/index.html')) throw new Error(`Falta ${path.join(RAIZ, 'index.html')}: compile con "npx ng build --configuration e2e"`);
  archivos = leidos;
  return leidos;
}

export const test = base.extend<{ aplicacion: void }>({
  aplicacion: [
    async ({ context, baseURL }, use) => {
      const origen = new URL(baseURL ?? 'http://localhost:4300').origin;
      const app = compilacion();
      await context.route(
        (url) => url.origin === origen && !url.pathname.startsWith('/api/'),
        async (route) => {
          let pathname: string;
          try {
            pathname = decodeURIComponent(new URL(route.request().url()).pathname);
          } catch {
            pathname = '/';
          }
          const archivo = app.get(pathname);
          if (archivo) {
            await route.fulfill({ status: 200, contentType: archivo.tipo, body: archivo.contenido });
          } else if (TIPOS[path.extname(pathname).toLowerCase()]) {
            await route.fulfill({ status: 404, contentType: TIPOS['.txt'], body: 'Not found' });
          } else {
            const indice = app.get('/index.html') as Archivo;
            await route.fulfill({ status: 200, contentType: indice.tipo, body: indice.contenido });
          }
        },
      );
      await use();
    },
    { auto: true },
  ],
});

export { expect };
