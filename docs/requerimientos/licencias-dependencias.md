# Licencias de dependencias — Portal 2.0 v4

Regla del plan: **solo recursos y librerías open source con licencia aprobada por la OSI** (MIT, Apache-2.0, BSD, MPL-2.0; fuentes con OFL-1.1). No se usan librerías de pago ni con licencia restringida o comunitaria (por ejemplo QuestPDF, iText, PyMuPDF/AGPL, MinIO server/AGPL, ag-Grid Enterprise, MediatR ≥ 13, FluentAssertions ≥ 8).

Cada fase que agrega una dependencia registra aquí el nombre, la **versión exacta instalada**, la licencia y la fase.

## Existentes

| Dependencia | Versión | Dónde | Licencia | Estado |
|---|---|---|---|---|
| Mermaid | 11.16.1 | Incrustada en `docs/documentacion.html` | MIT | Se mantiene |
| bpmn-js navigated-viewer | 17.11.1 | Incrustada en `docs/documentacion.html` | bpmn.io License (no OSI, exige marca de agua) | Se elimina en la Fase 7 (reemplazo por Mermaid) |
| MediatR | 12.4.1 | Backend | Apache-2.0 | No se actualiza a ≥ 13 (comercial) |
| FluentAssertions | 7.2.0 | Pruebas backend | Apache-2.0 | No se actualiza a ≥ 8 (comercial) |

## Agregadas por el plan

| Dependencia | Versión exacta | Dónde | Licencia | Fase |
|---|---|---|---|---|
| python-docx | 1.2.0 | `scripts/requerimientos/requirements.txt` | MIT | 0 |
| openpyxl | 3.1.5 | `scripts/requerimientos/requirements.txt` | MIT | 0 |
| openapi-spec-validator | 0.9.0 | `docs/integraciones/requirements.txt` | Apache-2.0 | 6a |
| Microsoft.Extensions.Http.Resilience | 9.10.0 | `HapagPortal.Infrastructure.csproj` | MIT | 6c |
| Polly.Core, Polly.Extensions, Polly.RateLimiting (transitivas de Microsoft.Extensions.Http.Resilience) | 8.4.2 | `HapagPortal.Infrastructure` | BSD-3-Clause | 6c |
| Microsoft.Extensions.Resilience, Microsoft.Extensions.Http.Diagnostics, Microsoft.Extensions.Telemetry (transitivas) | 9.10.0 | `HapagPortal.Infrastructure` | MIT | 6c |
| System.Threading.RateLimiting (transitiva de Polly.RateLimiting) | 8.0.0 | `HapagPortal.Infrastructure` | MIT | 6c |
| Microsoft.AspNetCore.Mvc.Testing (incluye Microsoft.AspNetCore.TestHost 9.0.20) | 9.0.20 | `backend/tests/HapagPortal.IntegrationTests` | MIT | 6c |
| schemathesis | 4.29.3 | `docs/integraciones/requirements.txt` | MIT | 6c |
| hypothesis (transitiva de schemathesis) | 6.168.4 | `docs/integraciones/requirements.txt` (vía schemathesis) | MPL-2.0 | 6c |
| Bootstrap (CSS y JS bundle, CDN cdn.jsdelivr.net con SRI) | 5.3.8 | `docs/prototipo/portal-2.0-prototipo.html` | MIT | 4 |
| Bootstrap Icons (CDN cdn.jsdelivr.net con SRI) | 1.13.1 | `docs/prototipo/portal-2.0-prototipo.html` | MIT | 4 |
| Inter (Google Fonts) | versión servida por fonts.googleapis.com | `docs/prototipo/portal-2.0-prototipo.html` | OFL-1.1 | 4 |
| Montserrat (Google Fonts) | versión servida por fonts.googleapis.com | `docs/prototipo/portal-2.0-prototipo.html` | OFL-1.1 | 4 |
| @playwright/test | 1.63.0 | `scripts/requerimientos/prototipo/package.json` | Apache-2.0 | 4 |
| playwright, playwright-core (transitivas de @playwright/test) | 1.63.0 | `scripts/requerimientos/prototipo/package-lock.json` | Apache-2.0 | 4 |
| @axe-core/playwright | 4.13.0 | `scripts/requerimientos/prototipo/package.json` | MPL-2.0 | 4 |
| axe-core (transitiva de @axe-core/playwright) | 4.13.0 | `scripts/requerimientos/prototipo/package-lock.json` | MPL-2.0 | 4 |
| angular-eslint (fijado en 21.x: la 22 exige Angular 22) | 21.4.0 | `frontend/package.json` (devDependencies) | MIT | 5a |
| @angular-eslint/builder, eslint-plugin, eslint-plugin-template, template-parser, schematics (transitivas de angular-eslint) | 21.4.0 | `frontend/package-lock.json` | MIT | 5a |
| eslint | 10.12.0 | `frontend/package.json` (devDependencies) | MIT | 5a |
| typescript-eslint | 8.59.2 | `frontend/package.json` (devDependencies) | MIT | 5a |
| @typescript-eslint/parser, @typescript-eslint/eslint-plugin (transitivas de typescript-eslint) | 8.59.2 | `frontend/package-lock.json` | MIT | 5a |
| @playwright/test | 1.63.0 | `frontend/package.json` (devDependencies) | Apache-2.0 | 5a |
| playwright, playwright-core (transitivas de @playwright/test) | 1.63.0 | `frontend/package-lock.json` | Apache-2.0 | 5a |
| @axe-core/playwright | 4.13.0 | `frontend/package.json` (devDependencies) | MPL-2.0 | 5a |
| axe-core (transitiva de @axe-core/playwright) | 4.13.0 | `frontend/package-lock.json` | MPL-2.0 | 5a |
| @jsverse/transloco (peerDependencies: `@angular/core >=16`, acepta Angular 21) | 8.4.0 | `frontend/package.json` (dependencies) | MIT | 5b |
| @jsverse/transloco-messageformat (plurales ICU, guía UI/a11y/i18n §5.5) | 8.4.0 | `frontend/package.json` (dependencies) | MIT | 5b |
| @jsverse/transloco-utils (transitiva de @jsverse/transloco) | 8.4.0 | `frontend/package-lock.json` | MIT | 5b |
| @jsverse/utils (peer de @jsverse/transloco-messageformat, instalada por npm) | 1.0.0-beta.5 | `frontend/package-lock.json` | MIT | 5b |
| @messageformat/core (transitiva de @jsverse/transloco-messageformat) | 3.4.0 | `frontend/package-lock.json` | MIT | 5b |
| @messageformat/parser, @messageformat/runtime, @messageformat/date-skeleton, @messageformat/number-skeleton (transitivas de @messageformat/core) | 5.1.1, 3.0.2, 1.1.0, 1.2.0 | `frontend/package-lock.json` | MIT | 5b |
| make-plural (transitiva de @messageformat/core) | 7.5.0 | `frontend/package-lock.json` | Unicode-DFS-2016 | 5b |
| moo (transitiva de @messageformat/parser) | 0.5.3 | `frontend/package-lock.json` | BSD-3-Clause | 5b |
| safe-identifier (transitiva de @messageformat/core) | 0.4.2 | `frontend/package-lock.json` | ISC | 5b |
| cosmiconfig 8.3.6 y sus transitivas (js-yaml 4.3.2, import-fresh, parse-json, etc.: MIT; argparse 2.0.1: Python-2.0), vía @jsverse/transloco-utils; solo Node, no entran al bundle | 8.3.6 | `frontend/package-lock.json` | MIT / Python-2.0 | 5b |
| @jsverse/transloco-keys-manager | 8.1.1 | `frontend/package.json` (devDependencies) | MIT | 5b |
| Transitivas de @jsverse/transloco-keys-manager (cheerio, glob, ora, chalk, cosmiconfig 9.0.2, @jsverse/angular-utils 1.0.0-beta.6, etc.: MIT; css-select, css-what, cheerio-select, entities: BSD-2-Clause; flat 6.0.1, ieee754: BSD-3-Clause) | según `package-lock.json` | `frontend/package-lock.json` (dev) | MIT / BSD-2-Clause / BSD-3-Clause | 5b |
