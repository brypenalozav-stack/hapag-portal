# Instructivo de Deploy en Railway

## Prerequisitos

- Cuenta en [Railway](https://railway.app) (plan Hobby $5/mes o superior)
- Repo GitHub: `brypenalozav-stack/hapag-portal` conectado a Railway
- PR mergeado a `develop`

---

## Paso 1: Crear el Proyecto en Railway

1. Ir a [railway.app/new](https://railway.app/new)
2. Click **"Deploy from GitHub repo"**
3. Seleccionar `brypenalozav-stack/hapag-portal`
4. Railway creara un proyecto vacio -- NO hacer deploy aun

---

## Paso 2: Agregar PostgreSQL

1. Dentro del proyecto, click **"+ New"** -> **"Database"** -> **"PostgreSQL"**
2. Railway crea la instancia y auto-genera las variables:
   - `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`
   - `DATABASE_URL` (connection string completa)
3. Anotar que estas variables estan disponibles para los otros servicios

---

## Paso 3: Configurar el Backend (.NET API)

1. Click **"+ New"** -> **"GitHub Repo"** -> seleccionar `hapag-portal`
2. En la configuracion del servicio:
   - **Service Name**: `backend`
   - **Root Directory**: `/backend`
   - **Builder**: Dockerfile
   - **Dockerfile Path**: `Dockerfile` (relativo al root directory)
   - **Watch Paths**: `/backend/**`
3. En **Variables**, agregar:

   ```
   # Railway auto-inyecta PGHOST, PGPORT, etc. desde PostgreSQL
   # Solo necesitas referenciarlas:
   PGHOST=${{Postgres.PGHOST}}
   PGPORT=${{Postgres.PGPORT}}
   PGDATABASE=${{Postgres.PGDATABASE}}
   PGUSER=${{Postgres.PGUSER}}
   PGPASSWORD=${{Postgres.PGPASSWORD}}

   # JWT Secret (generar uno seguro)
   Jwt__Secret=TU_SECRET_KEY_MINIMO_32_CARACTERES_AQUI

   # CORS (URL del frontend, se obtiene despues del paso 4)
   Cors__AllowedOrigins__0=https://TU-FRONTEND.up.railway.app

   # Entorno
   ASPNETCORE_ENVIRONMENT=Staging
   ```

4. En **Networking**, habilitar:
   - **Public Networking**: generar dominio (ej: `backend-xxx.up.railway.app`)
   - **Private Networking**: se auto-asigna `backend.railway.internal`

5. Click **Deploy**

---

## Paso 4: Configurar el Frontend (Angular + Nginx)

1. Click **"+ New"** -> **"GitHub Repo"** -> seleccionar `hapag-portal`
2. En la configuracion del servicio:
   - **Service Name**: `frontend`
   - **Root Directory**: `/frontend`
   - **Builder**: Dockerfile
   - **Dockerfile Path**: `Dockerfile`
   - **Watch Paths**: `/frontend/**`
3. En **Variables**, agregar:

   ```
   # URL interna del backend (Railway private networking)
   API_URL=http://backend.railway.internal:8080

   # Puerto (Railway lo inyecta, pero por si acaso)
   PORT=8080
   ```

4. En **Networking**, habilitar:
   - **Public Networking**: generar dominio (ej: `frontend-xxx.up.railway.app`)
   - Opcionalmente, configurar **Custom Domain** (ej: `portal.hapag-lloyd.cl`)

5. Click **Deploy**

---

## Paso 5: Actualizar CORS del Backend

Una vez que el frontend tenga su URL publica:

1. Ir al servicio **backend** -> **Variables**
2. Actualizar `Cors__AllowedOrigins__0` con la URL real del frontend:
   ```
   Cors__AllowedOrigins__0=https://frontend-xxx.up.railway.app
   ```
3. El backend se re-deploya automaticamente

---

## Paso 6: Regenerar Migraciones EF Core (primera vez)

Las migraciones SQL Server fueron eliminadas. Hay que regenerarlas para PostgreSQL.

### Opcion A: Localmente (recomendado)

```bash
# Clonar el repo
git clone https://github.com/brypenalozav-stack/hapag-portal.git
cd hapag-portal/backend

# Tener PostgreSQL local corriendo (Docker o instalado)
# docker run -d --name pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16

# Instalar herramientas EF
dotnet tool install --global dotnet-ef

# Generar migracion inicial
dotnet ef migrations add InitialCreate \
  -p src/HapagPortal.DatabaseMigrations \
  -s src/HapagPortal.WebApi

# Verificar que se crearon los archivos en:
# src/HapagPortal.DatabaseMigrations/Migrations/

# Aplicar migracion localmente para verificar
dotnet ef database update \
  -p src/HapagPortal.DatabaseMigrations \
  -s src/HapagPortal.WebApi

# Commit y push
git add .
git commit -m "feat(db): add PostgreSQL initial migration"
git push
```

### Opcion B: Railway ejecuta automaticamente

Si el `docker-entrypoint.sh` del backend detecta que no hay migraciones pendientes,
simplemente inicia la API. Las migraciones se ejecutan en cada deploy.

---

## Paso 7: Verificar el Deploy

1. **Backend**: abrir `https://backend-xxx.up.railway.app/swagger`
   - Debe mostrar Swagger UI con todos los endpoints
2. **Frontend**: abrir `https://frontend-xxx.up.railway.app`
   - Debe mostrar la pagina de login
3. **Demo**: el archivo `demo/index.html` no se deploya (es standalone, abrir local)

---

## Variables de Entorno - Resumen

### Backend
| Variable | Valor | Descripcion |
|---|---|---|
| `PGHOST` | `${{Postgres.PGHOST}}` | Host PostgreSQL (auto) |
| `PGPORT` | `${{Postgres.PGPORT}}` | Puerto PostgreSQL (auto) |
| `PGDATABASE` | `${{Postgres.PGDATABASE}}` | Nombre BD (auto) |
| `PGUSER` | `${{Postgres.PGUSER}}` | Usuario BD (auto) |
| `PGPASSWORD` | `${{Postgres.PGPASSWORD}}` | Password BD (auto) |
| `Jwt__Secret` | (generar) | Clave JWT min 32 chars |
| `Cors__AllowedOrigins__0` | URL frontend | CORS permitido |
| `ASPNETCORE_ENVIRONMENT` | `Staging` | Entorno ASP.NET |

### Frontend
| Variable | Valor | Descripcion |
|---|---|---|
| `API_URL` | `http://backend.railway.internal:8080` | Backend interno |
| `PORT` | `8080` | Puerto Nginx |

---

## Costos Estimados Railway

| Servicio | RAM | CPU | Costo aprox/mes |
|---|---|---|---|
| PostgreSQL | 512 MB | Shared | ~$5 |
| Backend (.NET) | 512 MB | Shared | ~$5 |
| Frontend (Nginx) | 256 MB | Shared | ~$2 |
| **Total** | | | **~$12/mes** |

Plan Hobby ($5/mes) incluye $5 de credito + $5 de uso.
Para produccion, considerar plan Pro ($20/mes).

---

## Troubleshooting

### "Connection refused" en el frontend
- Verificar que `API_URL` apunta al servicio backend con private networking
- El backend debe tener private networking habilitado

### "Migration failed" en el backend
- Verificar que las variables PG* estan correctamente referenciadas
- Revisar logs del backend en Railway dashboard
- Puede ser que las migraciones no se han generado aun (ver Paso 6)

### "502 Bad Gateway"
- El backend puede estar iniciando (toma ~15-30s en .NET)
- Verificar health check en Railway: el backend responde en `/swagger`

### CORS errors
- Actualizar `Cors__AllowedOrigins__0` con la URL exacta del frontend (con https://)
- No incluir trailing slash

---

## Archivar Repos Antiguos

Una vez que el monorepo este funcionando:

```bash
# Archivar repos viejos (no se pueden borrar si tienen PRs)
gh repo archive brypenalozav-stack/hapag-portal-frontend --yes
gh repo archive brypenalozav-stack/hapag-portal-backend --yes
```
