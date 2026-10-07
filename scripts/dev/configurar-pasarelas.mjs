// Configura las pasarelas de pago para depurar en local a partir de credenciales-prueba.json (fuera de git).
//   node configurar-pasarelas.mjs modos     -> escribe pasarelas-env.cmd con las variables de modo (antes de iniciar la API)
//   node configurar-pasarelas.mjs secretos  -> guarda las credenciales en el almacén cifrado del portal (API ya iniciada)
// Una pasarela con todos sus campos llenos queda en modo Real contra su ambiente de pruebas; el resto, en Dummy (simulador).
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const dir = dirname(fileURLToPath(import.meta.url));
const archivo = join(dir, 'credenciales-prueba.json');
const api = process.env.API_URL ?? 'http://localhost:5072';

if (!existsSync(archivo)) {
  console.log('Sin credenciales-prueba.json: todas las pasarelas quedan con el simulador.');
  if (process.argv[2] === 'modos') writeFileSync(join(dir, 'pasarelas-env.cmd'), '@rem sin credenciales de prueba\r\n');
  process.exit(0);
}

const config = JSON.parse(readFileSync(archivo, 'utf8'));
const secretos = (p) => Object.entries(p).filter(([k]) => /^[A-Z0-9_]+$/.test(k));
const completas = Object.entries(config)
  .filter(([nombre, p]) => !nombre.startsWith('_') && typeof p === 'object')
  .filter(([, p]) => secretos(p).length > 0 && secretos(p).every(([, v]) => typeof v === 'string' && v.trim() !== ''));

if (process.argv[2] === 'modos') {
  const lineas = ['@rem Generado por configurar-pasarelas.mjs'];
  for (const [nombre, p] of completas) {
    lineas.push(`set "Integrations__${nombre}__Mode=Real"`);
    if (p.BaseUrl) lineas.push(`set "Integrations__${nombre}__BaseUrl=${p.BaseUrl}"`);
  }
  writeFileSync(join(dir, 'pasarelas-env.cmd'), lineas.join('\r\n') + '\r\n');
  const reales = completas.map(([n]) => n);
  console.log(reales.length
    ? `Pasarelas reales (ambiente de pruebas): ${reales.join(', ')}. El resto usa el simulador.`
    : 'Ninguna pasarela con credenciales: todas usan el simulador.');
  process.exit(0);
}

if (process.argv[2] === 'secretos') {
  if (completas.length === 0) process.exit(0);
  const login = await fetch(`${api}/api/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: 'admin@hapag-lloyd.cl', password: 'Admin123!' }),
  });
  if (!login.ok) {
    console.log(`[AVISO] No se pudo iniciar sesión como administrador (${login.status}); las credenciales no se guardaron.`);
    process.exit(0);
  }
  const { token, accessToken } = await login.json();
  for (const [nombre, p] of completas) {
    for (const [type, value] of secretos(p)) {
      const r = await fetch(`${api}/api/v1/configuration/secrets`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token ?? accessToken}` },
        body: JSON.stringify({ scope: 'Global', clientId: null, type, value, expiresAt: null }),
      });
      if (!r.ok) console.log(`[AVISO] ${nombre}: no se pudo guardar ${type} (${r.status}).`);
    }
    console.log(`${nombre}: credenciales de prueba guardadas.`);
  }
  process.exit(0);
}

console.log('Uso: node configurar-pasarelas.mjs modos|secretos');
