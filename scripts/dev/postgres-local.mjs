// PostgreSQL local para depurar el portal (embedded-postgres, MIT). No requiere Docker ni instalar PostgreSQL.
// Datos persistentes en scripts/dev/.pgdata. Se detiene con Ctrl+C o cerrando la ventana.
import EmbeddedPostgres from 'embedded-postgres';
import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const dir = join(dirname(fileURLToPath(import.meta.url)), '.pgdata');
const pg = new EmbeddedPostgres({
  databaseDir: dir,
  user: 'postgres',
  password: 'postgres',
  port: 5432,
  persistent: true,
  initdbFlags: ['--encoding=UTF8', '--locale=C'],
});

if (!existsSync(join(dir, 'PG_VERSION'))) {
  console.log('Inicializando PostgreSQL local (solo la primera vez)...');
  await pg.initialise();
}
await pg.start();
try {
  await pg.createDatabase('HapagPortalDb');
} catch {
  // la base ya existe
}
console.log('PostgreSQL listo en localhost:5432 (base HapagPortalDb). Deje esta ventana abierta.');

const parar = async () => {
  await pg.stop();
  process.exit(0);
};
process.on('SIGINT', parar);
process.on('SIGTERM', parar);
setInterval(() => {}, 1 << 30);
