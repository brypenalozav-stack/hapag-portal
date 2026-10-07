# CT-STORAGE – Almacenamiento de archivos

| Campo | Valor |
|---|---|
| Contrato | CT-STORAGE |
| Estado | PROPUESTA – pendiente de validación con Área Seguridad TI (titular) |
| Responsable | Área Seguridad TI (titular) |
| Puerto | `IFileStorage` (Fase 6b; en el plan solo hay adaptador Dummy en memoria) |
| Fichas | M1-07 (documentos del registro), M5-06 (comprobante de depósito), M6-09 (repositorio documental), NF-07, NF-16 |
| Tareas de Pendientes | Sin tarea |

## Propósito

Hoy el portal no almacena archivos: el PDF de recibo y de ODS es un texto fijo y el campo `Payment.DepositProofUrl` no tiene carga. Este contrato define la interfaz que usa el portal para guardar, leer y borrar archivos, de modo que el almacenamiento real se pueda elegir después sin cambiar la lógica de negocio.

No es un contrato HTTP: describe el puerto interno y los requisitos que debe cumplir el almacenamiento real.

## Operaciones del puerto

| Operación | Firma propuesta (Fase 6b) | Resultado |
|---|---|---|
| Save | `SaveAsync(Stream content, string fileName, string contentType, string container)` | `Result<string>` con la clave del archivo |
| OpenRead | `OpenReadAsync(string key)` | `Result<Stream?>`; `null` si la clave no existe |
| Delete | `DeleteAsync(string key)` | `Result`; borrar una clave inexistente no es error |

Las fallas del almacenamiento se devuelven como `DomainErrors.Integration.Unavailable`, `Timeout` o `NotConfigured` (Fase 6b), nunca como excepción hacia el handler.

## Contenedores

| Contenedor | Contenido | Ficha |
|---|---|---|
| `registro` | Documentos de la cuenta de organización | M1-07 |
| `comprobantes` | Boletas y comprobantes de depósito adjuntados por el cliente | M5-06 |
| `documentos` | Certificados, cartas, cupones y copias de BL emitidos por el portal | M6-01 a M6-09 |

## Reglas propuestas

- **Clave:** `<contenedor>/<aaaa>/<mm>/<uuid>`. El nombre original se guarda como metadato, no en la clave.
- **Tipos admitidos:** `application/pdf`, `image/png` e `image/jpeg`. El portal valida el tipo por contenido, no solo por extensión.
- **Tamaño máximo:** 10 MB por archivo (valor por confirmar).
- **Acceso:** solo a través del backend del portal, que aplica los permisos de M1-11 y registra cada descarga (M6-09). No se publican URL públicas permanentes.
- **Cifrado:** en tránsito (TLS) y en reposo (NF-07).
- **Conservación:** según NF-16; el borrado físico sigue la política que defina Seguridad TI.
- **Credencial:** secreto `STORAGE_ACCESS_KEY` (Fase 6b), cifrado en el portal (NF-09).

## Opciones de implementación real (a decidir por Seguridad TI)

| Opción | Observación |
|---|---|
| Servicio gestionado de objetos compatible con S3 | Requiere cuenta y región aprobadas por Seguridad TI |
| Azure Blob Storage | Requiere suscripción de Hapag-Lloyd |
| Volumen persistente del contenedor de la API | Solo para ambientes de prueba |

Cualquier componente autoalojado debe tener licencia OSI permisiva; MinIO server queda excluido por su licencia AGPL (ver `docs/requerimientos/licencias-dependencias.md`).

## Puntos a validar

1. Opción de almacenamiento y región.
2. Tamaño máximo y tipos admitidos.
3. Plazo de conservación por contenedor (NF-16).
4. Procedimiento de respaldo y recuperación (NF-13).
