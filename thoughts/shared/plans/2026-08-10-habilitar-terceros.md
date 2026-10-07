# Habilitar Terceros por Bill of Lading — Plan de implementación

## Overview

Implementar en el Portal de Servicios el módulo **Habilitar Terceros** descrito en
`Requerimiento_Funcional_Habilitar_Terceros_Hapag_Lloyd.docx` (agosto de 2026).
El módulo permitirá que una empresa vinculada a un Bill of Lading (Shipper o
Consignee) otorgue a otra empresa un poder digital **limitado, trazable,
revocable y con vigencia** para ejecutar operaciones concretas sobre ese BL.

La autorización se compone de dos niveles separados:

1. **Relación entre empresas:** aceptación de Términos y Condiciones y, para la
   modalidad anual, un Contrato Marco Anual Digital vigente.
2. **Delegación operacional:** `empresa autorizante + empresa tercera + BL/alcance
   operacional + operaciones permitidas + vigencia`.

La autorización no transfiere la titularidad del BL, no amplía los permisos
globales del usuario y no permite inferir acceso a documentos u operaciones no
otorgados expresamente.

## Fuentes y alcance

- Fuente funcional: `I:\Documentos\Descargas\Requerimiento_Funcional_Habilitar_Terceros_Hapag_Lloyd.docx`.
- Arquitectura y hoja de ruta existente: `thoughts/shared/plans/2026-08-10-plataforma-operativa-aduana.md`.
- Código actual revisado: entidades `User`, `Client`, `BillOfLading`, pagos,
  demurrage, órdenes de servicio y cambio de almacén; `ICurrentUserService`,
  autorización por claims, controladores V1 y rutas Angular.
- Este plan cubre RF-001 a RF-020. No implementa integraciones externas ni crea
  operaciones Gate In/Out, Detention o Liberación que todavía no existan como
  flujos ejecutables en el repositorio. Sí deja su catálogo preparado y solo las
  marca como activas/delegables cuando el módulo correspondiente exista.

### Investigación funcional y normativa

- El estándar [DCSA Bill of Lading 3.0](https://dcsa.org/standards/bill-of-lading/documentation-bill-of-lading-3/bill-of-lading-3-introduction)
  estructura la interacción entre carrier, shipper y consignee/endorsee para
  emisión, modificación y surrender del documento. Su guía técnica contempla
  expresamente una parte que actúa
  [on behalf of consignee](https://developer.dcsa.org/implementing-bill-of-lading-si-td).
  Esto respalda modelar partes y representación mediante identificadores, no por
  nombres de texto libre.
- La guía oficial de [registro de empresas de Hapag-Lloyd](https://www.hapag-lloyd.com/en/online-business/olb-user-guide/user-data/company-registration-user-guide.html)
  indica que los usuarios trabajan asociados a la cuenta de su empresa. Esto es
  consistente con otorgar el poder a la organización y auditar al usuario que lo
  utiliza.
- La Resolución Exenta 5.739 de Aduanas regula el mandato para despachar,
  reconoce instrumentos electrónicos y establece, en los casos descritos, una
  [vigencia máxima de un año](https://www.bcn.cl/leychile/navegar?idNorma=1107781).
  Este módulo no sustituye automáticamente ese mandato aduanero, pero adopta su
  separación funcional entre relación/mandato y despachos concretos.
- La [Ley 19.799](https://www.bcn.cl/leychile/navegar?idNorma=196640) reconoce los
  documentos electrónicos y la firma electrónica bajo neutralidad tecnológica.
  Para aceptación contractual del portal se conservará evidencia de autenticación,
  intención, integridad y versión; cuando una operación aduanera exija firma
  electrónica avanzada, se integrará un prestador acreditado y no bastará el
  click de aceptación.
- La Ordenanza de Aduanas contempla conservación de antecedentes de operaciones
  aduaneras por [cinco años](https://www.bcn.cl/leychile/navegar?idNorma=120839).
  Se adopta cinco años como mínimo de auditoría operacional y contractual, sujeto
  a legal hold y a plazos superiores aplicables.
- La reforma chilena de protección de datos entra en vigor el 1 de diciembre de
  2026 y exige proporcionalidad, seguridad y conservación solo mientras resulte
  necesaria ([Ley 21.719 / texto diferido de la Ley 19.628](https://www.bcn.cl/leychile/navegar?idNorma=141599&idVersion=2026-12-01)).
  Por ello, la retención separa evidencia legal de telemetría y anonimiza datos
  técnicos cuando termina su finalidad.

## Definiciones funcionales confirmadas

- **Operaciones delegables iniciales:** consulta de estado de BL/contenedor, pago
  de Freight, Demurrage, Detention y Gate In/Out, solicitud de liberación y cambio
  de almacén.
- **No delegables por defecto:** descarga de BL, facturas y documentación
  comercial, y modificación/corrección de BL.
- **Matriz configurable:** Hapag-Lloyd podrá incorporar, restringir, activar o
  desactivar operaciones sin alterar el historial previo.
- **Acreditación del autorizante:** la base de datos cargará cada BL y sus
  entidades relacionadas. La posición Shipper/Consignee y las facultades se
  resuelven por BL; una misma empresa puede tener distinta posición o nivel de
  acceso en diferentes operaciones.
- **Alcance empresarial:** el poder se concede a toda la empresa tercera. Lo
  utilizan sus usuarios activos correctamente asociados, conservando trazabilidad
  individual de quién ejecutó cada acción.
- **Modalidades:** autorización por operación/BL y autorización anual. La relación
  anual entre empresas se formaliza mediante Contrato Marco Anual Digital,
  Términos y Condiciones versionados y aceptación electrónica de la empresa. Las
  operaciones delegadas se administran separadamente.
- **Regla adoptada para modalidad anual:** el contrato anual habilita la relación
  entre las dos empresas durante doce meses, pero no concede acceso automático a
  BL presentes o futuros. Cada BL requiere una delegación operacional explícita.
- **Facultad del usuario:** solo un usuario activo con poder empresarial
  `Delegations.Manage` puede otorgar, cambiar o revocar; `LegalTerms.Accept`
  habilita la aceptación contractual y `CompanyUsers.Manage` administra usuarios.

## Estado actual y brechas

### Capacidades reutilizables

- `User.ClientId` identifica la empresa con la que opera cada cuenta; el tercero
  puede usar su propia cuenta sin compartir credenciales.
- `BillOfLading.ClientId` modela hoy una única empresa propietaria del BL.
- Existen flujos de pagos, demurrage, consulta de BL/contenedores, cambio de
  almacén y órdenes de servicio.
- `BaseAuditableEntity` y `AuditableEntityInterceptor` registran cambios de
  persistencia.
- El JWT ya contiene `clientId`, roles y permisos, y los handlers pueden obtener
  el usuario actual mediante `ICurrentUserService`.
- `HasPermissionAttribute` permite proteger capacidades globales, pero no debe
  utilizarse como sustituto de la autorización por BL.

### Brechas que debe cerrar este módulo

- No existe una relación estructurada entre un BL y todas sus partes. Los campos
  `Shipper` y `Consignee` actuales son texto libre y `BillOfLading.ClientId`
  representa una sola empresa.
- No existe registro reutilizable de empresas terceras ni normalización de
  RUT/Tax ID para detectar duplicados.
- No existe catálogo configurable de operaciones delegables.
- No existe modelo de contrato marco anual, versión de Términos y Condiciones ni
  evidencia de aceptación electrónica por empresa.
- No existe autorización por recurso ni un punto central que todos los handlers
  deban consultar.
- La auditoría genérica no representa por sí sola el otorgamiento, modificación,
  revocación y uso efectivo del poder, ni captura IP/metadatos de la solicitud.
- Las consultas `my` filtran por empresa titular; deben incorporar acceso
  delegado sin filtrar datos sensibles de más.

## Decisiones de diseño

1. **Separar permisos globales y delegaciones.** Los claims del JWT responden
   “qué puede hacer normalmente este usuario”; una autorización vigente responde
   “qué puede hacer esta empresa sobre este BL”. Para operar como tercero deben
   cumplirse ambos requisitos de base que defina Hapag-Lloyd y la delegación por
   recurso; nunca se agregan todas las delegaciones al JWT.
2. **Validación en tiempo real en Application.** Crear `IBlOperationAuthorizer` y
   llamarlo dentro de cada query/command antes de leer datos o ejecutar acciones.
   Los guards, menús y atributos de controlador son defensa adicional, no la
   decisión final. No cachear autorizaciones inicialmente para que expiración y
   revocación sean inmediatas.
3. **Empresa, no usuario, como receptora.** La autorización se otorga a un
   `Client` tercero; cualquier usuario activo asociado a esa empresa opera con su
   propia identidad. El evento de ejecución conserva `UserId` y `ClientId`.
4. **Relación anual y operación son conceptos distintos.** Un contrato marco
   vigente formaliza la relación entre las empresas, pero no concede por sí solo
   acceso irrestricto a BL, documentos u operaciones. El acceso efectivo exige
   una delegación operacional que identifique su alcance y códigos permitidos.
5. **Partes del BL estructuradas.** Reutilizar `BLParty` del trabajo aduanero y
   asociar Shipper/Consignee con `ClientId` verificado. Mientras se migra el
   legado, no deducir facultades desde nombres de texto libre.
6. **Catálogo estable por código.** Las operaciones usan códigos inmutables; el
   nombre, categoría, disponibilidad, sensibilidad y condición delegable son
   configurables por Hapag-Lloyd. Desactivar un código impide nuevas
   autorizaciones sin borrar el historial.
7. **Historial append-only.** El grant mantiene su estado actual y una tabla de
   eventos conserva todos los cambios y ejecuciones, incluyendo actor, IP y
   metadatos técnicos seguros.
8. **Denegación por defecto y respuesta mínima.** La inexistencia, expiración,
   cierre, revocación o ausencia del código solicitado deniega el acceso. Una
   denegación no debe revelar si existe un BL ajeno.

## Modelo funcional propuesto

### Entidades nuevas

- `DelegableOperation`
  - `Id`, `Code` único, `Category`, `Name`, `IsDelegable`, `IsSensitive`,
    `IsActive`, `DisplayOrder`.
  - Seed inicial: `payment.freight`, `payment.demurrage`,
    `payment.detention`, `payment.gate-out`, `payment.gate-in`,
    `release.request`, `warehouse-change.request`, `gate.request`,
    `bl-status.read`, `container-status.read`, `payment-status.read`,
    `bl-document.download`, `invoice.download`,
    `commercial-document.download`, `bl-correction.request`.
  - Los códigos sin flujo actual quedan `IsActive = false`. Los cuatro códigos
    documentales sensibles quedan `IsDelegable = false` por defecto.
- `TermsAndConditionsVersion`
  - `Id`, `Version`, `Title`, `ContentHash`, `PublishedAt`, `EffectiveFrom`,
    `IsActive` y referencia inmutable al documento publicado.
- `CompanyTermsAcceptance`
  - `Id`, `ClientId`, `TermsVersionId`, `AcceptedByUserId`, `AcceptedAt`,
    `IpAddress?`, `UserAgent?`, `EvidenceHash`.
  - Una nueva versión de términos no sobrescribe aceptaciones anteriores.
- `CompanyUserAuthority`
  - `UserId`, `ClientId`, `AuthorityCode`, `GrantedByUserId`, `ValidFrom`,
    `ExpiresAt?`, `RevokedAt?` y `RowVersion`.
  - Códigos iniciales: `Delegations.Manage`, `LegalTerms.Accept` y
    `CompanyUsers.Manage`. La pertenencia a la empresa no basta para otorgar
    poderes; sí basta para consumir una delegación concedida a esa empresa cuando
    el usuario está activo.
- `AnnualThirdPartyAgreement`
  - `Id`, `AgreementNumber` único, `AuthorizerClientId`, `AuthorizedClientId`,
    `StartsAt`, `ExpiresAt`, `Status`, `TermsAcceptanceId`, `AcceptedByUserId`,
    `AcceptedAt`, `RevokedAt?`, `RevokedByUserId?`, `RowVersion`.
  - Estados: `PendingAcceptance`, `Active`, `Expired`, `Revoked`.
  - Vigencia máxima inicial de un año; renovación crea una nueva versión/acuerdo
    o un evento explícito según la política aprobada.
- `ThirdPartyAuthorization`
  - `Id`, `AuthorizationNumber` único y no predecible, `BillOfLadingId`,
    `AuthorizerClientId`, `AuthorizedClientId`, `GrantedByUserId`,
    `AnnualAgreementId?`, `AuthorizationType` (`PerBlOperation`/`Annual`),
    `AuthorizerPartyRole` (`Shipper`/`Consignee`), `ValidFrom`, `ExpiresAt`,
    `Status`, `RevokedAt?`, `RevokedByUserId?`, `RowVersion`.
  - Estados: `Active`, `Expired`, `Revoked`, `Closed`.
  - Una autorización anual debe referenciar un acuerdo anual activo y nunca puede
    exceder su vigencia. La delegación por BL mantiene el BL y permisos explícitos.
- `ThirdPartyAuthorizationPermission`
  - `AuthorizationId`, `DelegableOperationId`, `GrantedAt`, `GrantedByUserId`.
  - Clave única por autorización y operación.
- `ThirdPartyAuthorizationEvent`
  - `AuthorizationId`, `EventType`, `ActorUserId`, `ActorClientId`,
    `OperationCode?`, `OccurredAt`, `IpAddress?`, `UserAgent?`, `CorrelationId?`,
    `ChangesJson?`, `Outcome?`.
  - Eventos mínimos: `Created`, `PermissionsChanged`, `ValidityExtended`,
    `Revoked`, `Expired`, `Closed`, `OperationAllowed`, `OperationDenied`.

### Ajustes a entidades existentes

- `BLParty`: agregar `ClientId?` y estado de verificación para vincular la parte
  documental con una empresa registrada. Indexar por `BillOfLadingId`, `Role` y
  `ClientId`.
- `Client`: reutilizar como registro de empresa tercera; completar/validar
  `TaxId`, `TaxIdType`, `Name`, `Email`, `Phone`, `ClientType` y estado. Crear un
  tipo/catálogo de tercero (`Carrier`, `CustomsAgent`, `FreightForwarder`,
  `LogisticsOperator`, `Other`) sin duplicar empresas por rol comercial.
- `ApplicationDbContext`: agregar los ocho `DbSet`, relaciones, restricciones,
  índices y token de concurrencia.

## Plan de implementación

### Fase 0 — Baseline funcional versionado

Crear como configuración versionada la matriz `operación × rol BL × delegable ×
datos visibles × permiso global mínimo` con estas reglas iniciales:

| Operación | Shipper | Consignee | Datos visibles al tercero |
|---|---:|---:|---|
| Estado BL y contenedor | Sí | Sí | Identificadores, hitos, estado y datos operacionales mínimos |
| Pago Freight | Sí | Sí | Concepto, moneda, saldo y estado del pago |
| Pago Demurrage / Detention | Sí | Sí | Contenedor, período, cálculo, moneda, saldo y estado |
| Pago/Solicitud Gate In/Out | Sí | Sí | Contenedor, terminal, ventana, monto y estado |
| Solicitud de liberación | Sí | Sí | Requisitos y estado; documentos solo si otro permiso lo autoriza |
| Cambio de almacén | Sí | Sí | Origen, destino, costo y estado |
| Descargar BL/factura/documentos | No | No | Sin acceso |
| Modificar/corregir BL | No | No | Sin acceso |

- Ambas partes pueden delegar inicialmente, pero una regla configurada por código,
  país, cliente o estado del BL puede restringirlo sin despliegue.
- El autorizante debe tener `Delegations.Manage`; aceptar/renovar contrato exige
  `LegalTerms.Accept`.
- Contrato anual: doce meses desde `StartsAt`, nunca acceso automático a futuros
  BL, renovación explícita y nueva aceptación si cambió la versión de términos.
- Términos: documento versionado e inmutable, checkbox no preseleccionado,
  confirmación explícita autenticada, resumen descargable y recibo enviado a
  ambas empresas. Firma avanzada solo para operaciones cuya norma la exija.

Entregable: seed/configuración inicial, versión de la matriz y pruebas de contrato
que garanticen denegación por defecto.

### Fase 1 — Fundaciones de dominio y persistencia

1. Crear entidades, enums/constants y errores de dominio en
   `HapagPortal.Domain`.
2. Implementar configuraciones EF con:
   - índices únicos para `AuthorizationNumber`, `AgreementNumber`, versión de
     términos y `DelegableOperation.Code`;
   - restricción para impedir autorizante = autorizado;
   - unicidad de permisos por grant;
   - índices de consulta por `(AuthorizedClientId, Status, ExpiresAt)`, acuerdos
     por ambas empresas y vigencia,
     `(BillOfLadingId, AuthorizerClientId)` y Tax ID normalizado;
   - concurrencia optimista para edición/revocación.
3. Agregar migración `AddThirdPartyAuthorizations` en
   `HapagPortal.DatabaseMigrations` y seed del catálogo.
4. Incorporar el vínculo verificado `BLParty.ClientId`; crear un proceso de
   backfill solo para coincidencias inequívocas de Tax ID. Los registros de texto
   libre quedan pendientes de asociación manual y no pueden autorizar.
5. Añadir invariantes de dominio para aceptar términos, activar/renovar/revocar
   acuerdos anuales y crear, cambiar permisos, extender, revocar, expirar y cerrar
   delegaciones operacionales.
6. Incorporar poderes empresariales y seed de autoridades para administradores
   existentes, evitando que todo usuario de una empresa pueda otorgar poderes.

### Fase 2 — Registro y búsqueda de terceros

1. Crear queries/commands CQRS para buscar y registrar empresas terceras.
2. Normalizar RUT chileno (sin puntos/guion, DV mayúscula) y validar módulo 11;
   para otros países usar normalizador/validador por `Country + TaxIdType`.
3. Evitar duplicados mediante índice por `(Country, TaxIdType,
   NormalizedTaxId)`, no solo mediante validación de aplicación.
4. No activar automáticamente una cuenta de acceso al registrar la empresa.
   Reutilizar el flujo de invitación/confirmación de usuario y asociarlo al
   `ClientId` existente cuando se requiera acceso.
5. Estados de onboarding: `PendingVerification`, `Verified`, `Rejected`,
   `Suspended`. Validar RUT chileno por módulo 11 y unicidad normalizada; otros
   países usan `ITaxIdValidator` configurable. Las colisiones y países sin
   validador pasan a revisión administrativa.
6. Verificar el primer administrador mediante correo corporativo y aprobación del
   representante/poder empresarial; después, `CompanyUsers.Manage` invita y
   revoca usuarios. Suspender empresa o usuario corta el acceso inmediatamente.
7. Exponer endpoints V1 de búsqueda/registro con autorización global de cliente
   autenticado y controles de tenant.

### Fase 3 — Gestión del poder digital

1. Implementar búsqueda de BL delegables por número, booking, contenedor,
   nave/viaje y rango de fechas. Agregar `BookingNumber` indexado a
   `BillOfLading`; poblarlo junto con `Vessel` y `Voyage` desde la misma carga/base
   operacional que crea el BL, sin consultas a una fuente externa durante la
   autorización.
2. Crear `GetDelegableBillsQuery` que devuelva únicamente BL donde la empresa
   autenticada sea una parte Shipper/Consignee verificada y tenga facultad según
   la matriz.
3. Implementar:
   - `AcceptCompanyTermsCommand`;
   - `CreateAnnualThirdPartyAgreementCommand` y aceptación electrónica;
   - `GetMyAnnualAgreementsQuery`, renovación y revocación;
   - `CreateThirdPartyAuthorizationCommand`;
   - `GetMyGrantedAuthorizationsQuery` y detalle;
   - `UpdateAuthorizationPermissionsCommand`;
   - `ExtendAuthorizationCommand`;
   - `RevokeAuthorizationCommand`.
4. En creación y modificación validar en una misma transacción:
   - identidad/facultad del autorizante;
   - empresa tercera activa y distinta;
   - BL abierto y operación todavía ejecutable;
   - códigos activos y delegables;
   - aceptación de la versión vigente de Términos y Condiciones;
   - para modalidad anual, contrato marco aceptado/activo y vigencia de la
     delegación contenida dentro del período anual;
   - fechas UTC y vigencia futura;
   - unicidad de un grant activo por `BL + autorizante + tercero`. Nuevos permisos
     actualizan ese grant con control de concurrencia e historial; nunca se unen
     silenciosamente varios grants activos.
5. Generar número de autorización mediante servicio dedicado (por ejemplo
   `HT-2026-<token aleatorio>`) con índice único, sin exponer IDs secuenciales.
6. Registrar evento append-only y `AuditLog` en cada mutación, omitiendo datos
   sensibles innecesarios.
7. Conservar como evidencia la versión exacta de términos, identidad de empresa y
   usuario aceptante, timestamp UTC y metadatos técnicos; una actualización legal
   nunca modifica la evidencia histórica.
8. Renovación anual: crear una nueva versión de acuerdo enlazada al anterior; no
   extender en sitio. Las delegaciones por BL continúan únicamente si se
   revalidan contra el nuevo acuerdo y nunca más allá de su vencimiento.

### Fase 4 — Autorización transversal en tiempo real

1. Crear `IBlOperationAuthorizer.AuthorizeAsync(blId, operationCode, accessMode)`
   que produzca una decisión con `IsOwner`, `AuthorizationId?`, alcance de datos y
   motivo interno de denegación.
2. La decisión debe comprobar:
   - usuario y empresa activos;
   - acceso directo como parte válida del BL, o grant activo para la empresa;
   - permiso exacto, `ValidFrom/ExpiresAt`, revocación y estado de BL/operación;
   - configuración actual del código (`IsActive`, `IsDelegable`);
   - cuando corresponda, Contrato Marco Anual y aceptación electrónica vigentes;
   - permiso global mínimo cuando corresponda.
3. Integrarlo primero en los flujos existentes:
   - consulta de estado BL: `bl-status.read`;
   - consulta de contenedores: `container-status.read`;
   - consulta/creación de pagos existentes: código específico según concepto;
   - demurrage: `payment.demurrage` y consulta autorizada;
   - cambio de almacén: `warehouse-change.request`;
   - órdenes de servicio/liberación: código aplicable aprobado.
4. Separar DTOs de titular y tercero. Un permiso de estado no debe serializar
   documentos, facturas, parties completas, importes comerciales u otros datos
   fuera del alcance autorizado.
5. Reemplazar consultas basadas solo en `ClientId` por filtros que unan acceso
   directo y grants vigentes. Nunca cargar todos los BL para filtrar en memoria.
6. Registrar `OperationAllowed` junto con el `AuthorizationId` y la identidad
   efectiva. Registrar denegaciones con rate limiting y sin revelar información
   del BL en la respuesta.
7. Añadir un job idempotente de expiración/cierre para estado e historial, pero
   mantener la comprobación por reloj en cada decisión: el job no es requisito
   para denegar una autorización vencida.

### Fase 5 — API y contrato de errores

Crear `ThirdPartyAuthorizationsController` V1 con endpoints equivalentes a:

- `GET /third-party-authorizations/delegable-bills`
- `GET /third-parties?taxId=&name=`
- `POST /third-parties`
- `GET /delegable-operations`
- `GET /third-party-agreements/terms/current`
- `POST /third-party-agreements/terms/acceptances`
- `POST /third-party-agreements/annual`
- `POST /third-party-agreements/annual/{id}/accept`
- `POST /third-party-agreements/annual/{id}/revoke`
- `POST /third-party-authorizations`
- `GET /third-party-authorizations/granted-by-me`
- `GET /third-party-authorizations/granted-to-me`
- `GET /third-party-authorizations/{id}`
- `PUT /third-party-authorizations/{id}/permissions`
- `PUT /third-party-authorizations/{id}/validity`
- `POST /third-party-authorizations/{id}/revoke`

Reglas del contrato:

- `404` para recursos inexistentes o ajenos cuando revelar su existencia sea un
  riesgo; `403` para una operación conocida pero no autorizada desde una vista a
  la que el tercero ya accedió.
- `409` para concurrencia, duplicidad o estado cerrado; `422` para reglas de
  vigencia/matriz.
- Idempotency key en creación y revocación para evitar duplicados por reintento.
- Paginación y límites en búsquedas; timestamps ISO-8601 UTC.

### Fase 6 — Angular: Habilitar Terceros y experiencia del tercero

1. Añadir feature lazy `features/third-party-authorizations/` y servicios/modelos.
2. Flujo guiado del titular:
   - buscar y seleccionar BL;
   - seleccionar/registrar tercero;
   - elegir operaciones individualmente;
   - seleccionar modalidad por operación/BL o anual;
   - mostrar/aceptar Términos y Condiciones y, en modalidad anual, crear o
     seleccionar el Contrato Marco Anual Digital;
   - definir vigencia sin superar la del contrato anual;
   - revisar permisos otorgados/no otorgados;
   - confirmar y mostrar número de autorización.
3. Vista **Mis autorizaciones** con filtros, detalle, edición, extensión,
   revocación con confirmación y estado/vigencia visibles.
4. Vista **Relaciones anuales** con estado de aceptación, empresas, período,
   versión de términos, permisos asociados, renovación y revocación.
5. Vista **Autorizadas para mi empresa** para el tercero. Mostrar solo BL y
   acciones autorizadas; ocultar documentos sensibles y deshabilitar acciones
   vencidas, sin confiar en ese ocultamiento para seguridad.
6. Añadir guard/directiva para UX basada en la decisión de alcance del backend,
   no en una lista de grants almacenada indefinidamente en el navegador.
7. Tratar `409` de concurrencia recargando el detalle; mostrar mensaje funcional
   claro para autorización vencida/revocada/no otorgada.
8. Añadir navegación “Habilitar Terceros” y badges de terceros activos en los BL
   del titular.

### Fase 7 — Auditoría, observabilidad y seguridad

1. Pantalla/endpoint de historial para titular y administradores; el tercero ve
   únicamente eventos pertinentes a su empresa.
2. Capturar IP, user agent y correlation ID mediante una abstracción de contexto
   de solicitud; respetar proxy headers confiables y política de retención.
3. Métricas: grants creados/activos/revocados/vencidos, decisiones denegadas,
   operaciones por código y errores de concurrencia.
4. Alertas de abuso: enumeración de BL, denegaciones repetidas, intento de operar
   tras revocación y cambios masivos de permisos.
5. Revisión de seguridad específica: IDOR/BOLA, mass assignment, escalamiento
   horizontal entre empresas, tokens antiguos, carreras revocar/ejecutar,
   exposición por logs y exportaciones.
6. Notificaciones mediante outbox transaccional, correo e inbox del portal:
   - creación pendiente de aceptación y aceptación del contrato anual;
   - creación o cambio de permisos de una delegación;
   - revocación inmediata;
   - recordatorios 30, 7 y 1 día antes del vencimiento anual, y aviso al vencer;
   - ejecución de operaciones sensibles/pagos y cambio de administrador/poderes.
   Se notifica a administradores/poderes de ambas empresas y al actor. Un fallo de
   correo no revierte la acción; se reintenta y queda observable.
7. SLA funcional adoptado:
   - revocación/suspensión: efectiva en la siguiente autorización del backend, sin
     caché positiva; objetivo menor a 5 segundos desde confirmación;
   - cambios de permisos: misma garantía;
   - entrega de evento al outbox: atómica con la operación;
   - primer intento de notificación: objetivo menor a 5 minutos;
   - disponibilidad de historial en UI: inmediata tras confirmar la transacción.
8. Retención y acceso:
   - contrato, aceptación, grants y eventos que prueban operaciones: mínimo cinco
     años desde el 1 de enero siguiente al cierre/última operación, ampliable por
     legal hold o norma local;
   - denegaciones técnicas detalladas: 12 meses; después agregar/anonimizar;
   - IP y user agent: 12 meses salvo incidente/legal hold; conservar después solo
     hash o datos agregados cuando sea suficiente;
   - no borrar evidencia referencial al eliminar una cuenta: seudonimizar datos
     personales no necesarios y conservar identificadores legales exigibles;
   - titular y tercero acceden solo a su relación; administradores de cumplimiento
     requieren `ThirdPartyAudit.ReadAll`, con toda consulta también auditada.

### Fase 8 — Activación gradual y documentación

1. Feature flag global y por país/cliente.
2. Activar primero consultas de estado, después una operación de bajo riesgo y
   finalmente pagos/solicitudes, con catálogo controlado por Hapag-Lloyd.
3. Backfill/verificación de partes del BL antes de habilitar creación de grants.
4. Documentar modelo, secuencia de autorización, matriz, endpoints y guía de
   usuario en `docs/documentacion.html`, alineado con la Fase 9 del plan general.
5. Preparar rollback lógico: desactivar delegación sin borrar grants ni eventos.

## Estrategia de pruebas

### Dominio y Application

- Solo Shipper/Consignee verificado con facultad puede crear un grant.
- La posición y facultad se evalúan por BL; una empresa Shipper en un BL no
  obtiene facultades sobre otro BL donde no está relacionada.
- No se permite autorizar a la misma empresa, un tercero inactivo, un BL cerrado,
  códigos inexistentes/inactivos/no delegables ni una vigencia inválida.
- Un BL admite varios terceros y un tercero permisos distintos por BL.
- Todos los usuarios activos asociados a la empresa tercera comparten el alcance
  empresarial, pero cada ejecución conserva el `UserId` individual.
- Un acuerdo anual sin aceptación, vencido o revocado no habilita operaciones; un
  acuerdo activo tampoco concede operaciones fuera de la delegación explícita.
- Una delegación anual no puede sobrevivir a la fecha de término de su contrato
  marco ni a la versión/aceptación legal exigible.
- Solo `Delegations.Manage` puede otorgar/revocar y solo `LegalTerms.Accept` puede
  aceptar el contrato; un usuario común de la empresa no puede elevarse poderes.
- Modificar permisos conserva historial; extender no revive un grant revocado.
- Revocación y expiración deniegan inmediatamente sin esperar al job.
- Concurrencia entre ejecución/revocación produce una decisión coherente y
  auditable.
- El tercero puede ejecutar exactamente los códigos otorgados y ningún otro.
- El titular conserva su acceso normal; una delegación nunca modifica la
  titularidad ni los claims.

### Persistencia e integración HTTP

- Unicidad real de Tax ID normalizado, código y número de autorización.
- Evidencia inmutable de aceptación electrónica y versión exacta de términos.
- Renovación anual crea nueva versión, exige revalidación y no hereda acceso a
  futuros BL automáticamente.
- Aislamiento entre al menos tres empresas y dos BL.
- Pruebas BOLA modificando IDs en detalle, edición, revocación y ejecución.
- `404/403/409/422` según contrato, idempotencia y paginación.
- JWT emitido antes de una revocación no mantiene acceso delegado.
- DTO de consulta limitada no contiene BL/facturas/documentos sensibles.
- Cada operación permitida queda enlazada a autorización, tercero, usuario y
  autorizante; intentos denegados relevantes quedan trazados.

### Frontend y end-to-end

- Wizard completo, validación de tercero duplicado, resumen y confirmación.
- Selección de modalidad por BL/anual, aceptación electrónica y visualización del
  contrato marco asociado.
- Notificaciones por outbox en creación, cambios, revocación y vencimiento.
- Edición, extensión, revocación y actualización inmediata de acciones.
- Sesión independiente del tercero y visibilidad mínima por operación.
- Recorrido del caso ABC/Transportes XYZ descrito en el requerimiento.
- Accesibilidad por teclado, estados de carga/error y diseño responsive.

### Verificación automatizada

- `dotnet build backend/HapagPortal.sln -c Release`
- `dotnet test backend/HapagPortal.sln`
- Aplicación de migración sobre base vacía y copia representativa de desarrollo.
- `npx ng build --configuration production` desde `frontend/`.
- Tests de componentes/servicios Angular y suite E2E de autorización/revocación.

## Trazabilidad RF

| Requisitos | Cobertura principal |
|---|---|
| RF-001–RF-004 | Fases 3 y 6: módulo, relación verificada y búsqueda de BL |
| RF-005–RF-006 | Fase 2: selección/registro y unicidad de Tax ID |
| RF-007–RF-012 | Fases 1 y 3: catálogo, permisos, vigencia y multiplicidad |
| RF-013–RF-014 | Fase 4: autorización transversal y minimización de datos |
| RF-015–RF-017 | Fase 3: modificar, extender y revocar inmediatamente |
| RF-018–RF-019 | Fases 1 y 7: eventos append-only y auditoría técnica |
| RF-020 | Fases 2, 4 y 6: cuenta propia e identidad efectiva del ejecutor |

## Criterios de término

- El titular solo puede otorgar poderes sobre BL donde es parte verificada.
- El tercero entra con su cuenta y ve únicamente BL, datos y acciones concedidos.
- La modalidad anual mantiene separadas la relación contractual entre empresas y
  las operaciones efectivamente delegadas.
- Todas las operaciones integradas consultan `IBlOperationAuthorizer` dentro del
  handler, no solo en UI/controlador.
- Revocar o vencer un grant impide el siguiente uso incluso con un JWT previo.
- Documentos sensibles permanecen bloqueados por defecto y no aparecen en DTOs
  limitados.
- Existe historial completo de creación, cambios, extensión, revocación y usos.
- Los RF-001–RF-020 tienen al menos una prueba de aceptación trazable.
- Builds, tests, migraciones y recorrido E2E están verdes.

## Baseline implementable adoptado

No quedan pendientes funcionales bloqueantes para construir el módulo. Se adoptan
las siguientes decisiones, todas modificables por configuración o una iteración
de negocio posterior sin debilitar el modelo de seguridad:

- Shipper y Consignee pueden delegar el catálogo inicial; el usuario necesita
  `Delegations.Manage` y la matriz permite restricciones por operación/país.
- El contrato anual dura doce meses y formaliza la relación empresarial, pero
  cada BL requiere delegación operacional explícita; no existe herencia automática
  hacia BL futuros.
- La aceptación contractual usa firma electrónica simple reforzada: sesión con
  MFA para usuarios con poder legal, acción afirmativa, versión/hash inmutable,
  timestamp, IP, user agent, identidad de empresa/usuario y recibo. Firma
  electrónica avanzada se exige únicamente cuando la operación/norma aplicable lo
  requiera, especialmente mandatos aduaneros formales.
- `BookingNumber`, `Vessel` y `Voyage` se alimentan desde la carga/base operacional
  del BL y se indexan para búsqueda.
- El tercero se registra una sola vez por Tax ID normalizado, pasa por verificación
  empresarial, designa un primer administrador y gestiona invitaciones/poderes;
  usuarios y empresas suspendidos pierden acceso inmediatamente.
- Se notifican por inbox/correo los eventos contractuales, cambios, revocaciones,
  operaciones sensibles y vencimientos 30/7/1 días antes.
- Evidencia contractual/operacional se conserva al menos cinco años; telemetría
  personal detallada, doce meses. Legal hold prevalece. El acceso global requiere
  `ThirdPartyAudit.ReadAll` y también queda auditado.

Los textos legales definitivos, branding de correos, SLA contractual y ampliación
del catálogo son contenido/configuración de despliegue: requieren aprobación del
negocio o Legal antes de producción, pero no cambian las entidades, contratos de
API ni controles necesarios para implementar y probar el módulo.
