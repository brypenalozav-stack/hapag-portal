# Registro de autorrevisión del portugués de Brasil (pt-BR)

Registro de la autorrevisión de `frontend/public/i18n/pt.json`. El portugués lo redacta el implementador y no tiene revisor humano (plan pt-BR, Fase 2).

- Cada lote L1 a L8 tiene su fila, con el alcance (secciones), la fecha (AAAA-MM-DD), el criterio aplicado y el resultado.
- Además hay una fila por cada alcance que se relee completo: `common`, `auth`, `*.errors.*` y `acciones`, con resultado `revisado 100 %`.
- Criterio: glosario (`docs/requerimientos/glosario-es-en.md`), guía UI/a11y/i18n §5.7 y adaptación de voz y sujeto como en `en.json`.

`npm run check:i18n-pt` (regla (f)) solo comprueba que este registro exista y tenga el formato correcto. No prueba que la revisión haya ocurrido ni su calidad: la garantía automática de calidad son las reglas (a) a (e) del mismo chequeo.

| Lote | Alcance | Fecha | Criterio | Resultado |
|---|---|---|---|---|
| L1 | `common` | 2026-10-29 | glosario, §5.7, adaptación de voz y sujeto como en en.json | revisado 100 %, chequeo sin errores |
| L2 | `shared`, `app`, `status`, `announcementsView`, `faq`, `profile`, `paymentRedirect` | 2026-10-29 | glosario, §5.7, adaptación de voz y sujeto como en en.json | revisado 100 %, chequeo sin errores |
| L3 | `admin.accessMatrix` … `admin.finance` | 2026-10-29 | glosario, §5.7, adaptación de voz y sujeto como en en.json | revisado 100 %, chequeo sin errores |
| L4 | `admin.guides` … `admin.paymentMethods` | 2026-10-29 | glosario, §5.7, adaptación de voz y sujeto como en en.json | revisado 100 %, chequeo sin errores |
| L5 | `admin.publicationRules` … `admin.users` | 2026-10-29 | glosario, §5.7, adaptación de voz y sujeto como en en.json | revisado 100 %, chequeo sin errores |
| L6 | `shipments`, `documents`, `releaseLetterPage`, `releaseStatus`, `warehouse`, `tatcRequests`, `demurrage`, `dangerousGoods` | 2026-10-29 | glosario, §5.7, adaptación de voz y sujeto como en en.json | revisado 100 %, chequeo sin errores |
| L7 | `accountStatement` … `localTariffs` | 2026-10-29 | glosario, §5.7, adaptación de voz y sujeto como en en.json | revisado 100 %, chequeo sin errores |
| L8 | `serviceRequests` … `dashboard` | 2026-10-29 | glosario, §5.7, adaptación de voz y sujeto como en en.json | revisado 100 %, chequeo sin errores |
| Consistencia | `pt.json` completo: claves agrupadas por valor ES y por valor EN, unificaciones decididas (Situação, o guia, lançamento a crédito, Pagar com minha conta, Editar, papel/perfil, cadastro, matriz, Painel, fila, paginação, Recusar, Cancelar, Aguardando, Em andamento) | 2026-10-29 | glosario, §5.7, adaptación de voz y sujeto como en en.json | revisado 100 %, grupos unificados o justificados; chequeo sin errores |
| Relectura | `common` | 2026-10-29 | relectura completa: calcos, naturalidad, concordancia y tipografía (“ ”, % pegado, você) | revisado 100 % |
| Relectura | `auth` | 2026-10-29 | relectura completa: calcos, naturalidad, concordancia y tipografía (“ ”, % pegado, você) | revisado 100 % |
| Relectura | `*.errors.*` | 2026-10-29 | relectura completa: calcos, naturalidad, concordancia y tipografía (“ ”, % pegado, você) | revisado 100 % |
| Relectura | `acciones` | 2026-10-29 | relectura completa: calcos, naturalidad, concordancia y tipografía (“ ”, % pegado, você) | revisado 100 % |
