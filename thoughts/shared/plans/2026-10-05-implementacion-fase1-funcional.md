---
title: 2026-10-05-implementacion-fase1-funcional
type: plan
date: 2026-10-05
status: active
project: hapag-portal
scope: shared
author: brypenalozav-stack
ticket: null
tags: [plan, portal-2.0, fase-1, funcional]
related: [thoughts/shared/plans/2026-10-05-actualizacion-requerimientos-portal-2-0-ui-integraciones.md, thoughts/shared/plans/2026-08-10-habilitar-terceros.md]
---

# Plan: implementación de las fichas funcionales de Fase 1 (especificación v4)

## Overview

El plan anterior dejó la base: especificación v4, contratos e integraciones con adaptadores Dummy, i18n ES/EN y accesibilidad. Este plan construye las **fichas funcionales de Fase 1** de la especificación v4 (`docs/requerimientos/especificacion-funcional-v4.txt`), con prioridad a los módulos del documento.

- Fichas de Fase 1: 72 RF. M11 (8) ya está cubierto, salvo el selector de tema (M11-07). Quedan unas 60 fichas en M1–M10.
- Cobertura de partida: `katu\v4\Matriz_Fichas_Linea_Base_v4.xlsx` y `docs/requerimientos/matriz-trazabilidad.md`.

## Reglas

- Arquitectura existente: Clean Architecture, CQRS con MediatR 12.4.1, `Result<T>`, EF Core + PostgreSQL con migraciones en `HapagPortal.DatabaseMigrations`. Frontend Angular 21 standalone con signals, Transloco (toda cadena nueva en `es.json` y `en.json`) y la base de accesibilidad (lint en error, axe sin violaciones).
- Datos externos solo a través de los puertos de `Application/Common/Interfaces` (adaptadores Dummy hoy; Real cuando haya contrato validado). Nada de acceso directo a sistemas de origen.
- Control de permisos en el servidor (NF-05): toda consulta de BL, cargos, documentos y pagos filtra por los accesos del usuario.
- Solo dependencias open source (MIT, Apache-2.0, BSD, MPL); cada una se registra en `docs/requerimientos/licencias-dependencias.md`.
- Cada ola: rama desde `develop` (o desde la ola anterior si su PR no está mezclado), commit por bloque, criterios automáticos en verde (`dotnet test`, `ng lint`, `ng build`, checks y Playwright).

## Olas (orden por dependencias)

| Ola | Fichas | Contenido |
|---|---|---|
| A | M1-02, M1-04, M1-07, M1-08, M1-10, M1-11, M8-04, M8-06, M2-06, M2-07 | Organizaciones y tipo (cliente, FFWW, agencia de aduanas, transportista); registro con aprobación y vinculación de usuarios; roles por embarque (customer, shipper, consignee, tercero) y matriz base de permisos del capítulo M1-11 aplicada en servidor; Match Code; administrador con visibilidad total; logout en servidor; selector de país; listado y detalle de embarques con booking, filtros y separación importación/exportación |
| B | M1-03, M1-12–M1-18, M1-20, M1-22, M1-23, M1-24 | Accesos a terceros: otorgamiento individual, masivo y por defecto, vigencia, permisos granulares, ampliación entre roles, acceso abierto por BL, autoasociación, acceso por booking, revocación en cadena, auditoría y vista única (reutiliza `2026-08-10-habilitar-terceros.md`) |
| C | M8-01, M8-02, M8-03, M4-01–M4-04, M5-05, M3-01, M3-02, M3-04, M3-05, M3-16, M3-18 | Mantenedor de tarifas; lectura de crédito, FFWW, exenciones y tipo de cambio por puertos; reglas Gate In/EDS/Gate Out, IPO y carta FFWW; MHD, demurrage por estado, demoras anticipadas Bolivia; cambio de almacén gratuito y masivo |
| D | M5-01–M5-04, M5-07–M5-09, M8-07, M7-01, M7-02 | Carro unificado y separado por moneda, RUT de facturación, medios y monedas configurables, crédito, control de anulación, bloqueo de pagos por horario, facturas e historial |
| E | M6-01, M6-03–M6-07, M6-09 | Documentos PDF (PDFsharp/MigraDoc, MIT) con firma y almacenamiento vía puertos: certificado de transbordo, cupón Gate Out, comprobante Collect, copia de BL, carta de responsabilidad, CLD y repositorio documental |
| F | M1-01, M1-05, M2-01, M2-02, M2-05, M2-09, M10-01–M10-03, M10-05, M10-06, M11-07 | Dashboard consolidado, reglas de publicación, Dispute, TATC, asistente sobre puerto con adaptador Dummy (FAQ + consultas sobre datos con permisos), buscador DG, selector de tema |

## Qué NO hace este plan

- Fichas de Fase 2 y Fase 0.
- Adaptadores Real contra sistemas sin contrato validado.
- Modelo de IA de pago: el asistente usa un puerto con adaptador Dummy/reglas; un proveedor real se conecta después.

## Seguimiento

| Ola | Rama | Estado |
|---|---|---|
| A | `feature/fase1-ola-a-organizacion-acceso` | En curso |
| B | — | Pendiente |
| C | — | Pendiente |
| D | — | Pendiente |
| E | — | Pendiente |
| F | — | Pendiente |
