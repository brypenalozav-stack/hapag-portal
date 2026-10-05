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
