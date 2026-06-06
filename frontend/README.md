# Hapag-Lloyd Portal Frontend (hapag-portal)

Aplicacion SPA en Angular 21 para el portal de clientes de Hapag-Lloyd Chile/Bolivia.

## Tabla de Contenidos

- [Descripcion](#descripcion)
- [Stack Tecnologico](#stack-tecnologico)
- [Arquitectura](#arquitectura)
- [Modulos de Funcionalidad](#modulos-de-funcionalidad)
- [Requisitos Previos](#requisitos-previos)
- [Primeros Pasos](#primeros-pasos)
- [Build](#build)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Configuracion de Entornos](#configuracion-de-entornos)
- [Convenciones de Codigo](#convenciones-de-codigo)

## Descripcion

Frontend del portal de clientes de Hapag-Lloyd para las operaciones en Chile y Bolivia. Aplicacion de pagina unica (SPA) que consume la API REST del backend para gestionar documentos de embarque, pagos, cargos locales, sobreestadia, bodegas y mas.

## Stack Tecnologico

| Tecnologia  | Version |
| ----------- | ------- |
| Angular     | 21.2    |
| Bootstrap   | 5.3.8   |
| TypeScript  | 5.9     |
| Estilos     | SCSS    |

## Arquitectura

El proyecto sigue el patron **Core / Features / Shared** con componentes standalone:

- **Core**: Servicios singleton, guards, interceptors, modelos globales
- **Features**: Modulos de funcionalidad independientes (lazy-loaded)
- **Shared**: Componentes, directivas y pipes reutilizables

## Modulos de Funcionalidad

| Modulo           | Descripcion                                      |
| ---------------- | ------------------------------------------------ |
| `auth`           | Login, registro, recuperacion de contrasena       |
| `dashboard`      | Panel principal con metricas y resumen            |
| `bill-of-lading` | Consulta y gestion de documentos de embarque (BL) |
| `payments`       | Gestion y seguimiento de pagos                    |
| `local-charges`  | Consulta de cargos locales                        |
| `demurrage`      | Gestion de sobreestadia                           |
| `warehouse`      | Gestion de bodegas                                |
| `faq`            | Preguntas frecuentes                              |
| `admin`          | Administracion de usuarios y configuracion        |

## Requisitos Previos

- Node.js 22+
- npm 11+
- Angular CLI 21

## Primeros Pasos

1. **Instalar dependencias**:

   ```bash
   npm install
   ```

2. **Configurar la URL de la API** en `src/environments/environment.ts`:

   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'https://localhost:7062/api'
   };
   ```

3. **Iniciar el servidor de desarrollo**:

   ```bash
   npm start
   ```

   La aplicacion estara disponible en: http://localhost:4200

## Build

| Comando                | Entorno     |
| ---------------------- | ----------- |
| `npm run build:prod`   | Produccion  |
| `npm run build:staging`| Staging     |

Los artefactos de build se generan en el directorio `dist/`.

## Estructura del Proyecto

```
src/
├── app/
│   ├── core/                # Servicios, guards, interceptors, modelos
│   │   ├── guards/
│   │   ├── interceptors/
│   │   ├── models/
│   │   └── services/
│   ├── features/            # Modulos de funcionalidad
│   │   ├── auth/
│   │   ├── dashboard/
│   │   ├── bill-of-lading/
│   │   ├── payments/
│   │   ├── local-charges/
│   │   ├── demurrage/
│   │   ├── warehouse/
│   │   ├── faq/
│   │   └── admin/
│   └── shared/              # Componentes, directivas, pipes compartidos
│       ├── components/
│       ├── directives/
│       └── pipes/
├── environments/
│   ├── environment.ts           # Desarrollo
│   ├── environment.staging.ts   # Staging
│   └── environment.prod.ts      # Produccion
├── assets/
└── styles/
```

## Configuracion de Entornos

| Archivo                       | Entorno     | Descripcion                          |
| ----------------------------- | ----------- | ------------------------------------ |
| `environment.ts`              | Desarrollo  | Configuracion local por defecto      |
| `environment.staging.ts`      | Staging     | Configuracion para entorno de pruebas|
| `environment.prod.ts`         | Produccion  | Configuracion para produccion        |

Cada archivo de entorno define al menos:

```typescript
export const environment = {
  production: boolean,
  apiUrl: string
};
```

## Convenciones de Codigo

- **Signals** para manejo de estado reactivo
- **Computed** para estado derivado
- **takeUntilDestroyed** para gestionar suscripciones y evitar memory leaks
- **Reactive Forms** para todos los formularios
- **Standalone components** en lugar de NgModules
- Nombres de archivos en kebab-case: `feature-name.component.ts`
- Un componente por archivo

---

# Hapag-Lloyd Portal Frontend (hapag-portal)

Angular 21 SPA for the Hapag-Lloyd Chile/Bolivia customer portal.

## Table of Contents

- [Description](#description)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture-1)
- [Feature Modules](#feature-modules)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Build](#build-1)
- [Project Structure](#project-structure)
- [Environment Configuration](#environment-configuration)
- [Coding Conventions](#coding-conventions)

## Description

Frontend for the Hapag-Lloyd customer portal serving operations in Chile and Bolivia. A single-page application (SPA) that consumes the backend REST API to manage bills of lading, payments, local charges, demurrage, warehouses, and more.

## Tech Stack

| Technology  | Version |
| ----------- | ------- |
| Angular     | 21.2    |
| Bootstrap   | 5.3.8   |
| TypeScript  | 5.9     |
| Styles      | SCSS    |

## Architecture

The project follows the **Core / Features / Shared** pattern with standalone components:

- **Core**: Singleton services, guards, interceptors, global models
- **Features**: Independent feature modules (lazy-loaded)
- **Shared**: Reusable components, directives, and pipes

## Feature Modules

| Module           | Description                                  |
| ---------------- | -------------------------------------------- |
| `auth`           | Login, registration, password recovery       |
| `dashboard`      | Main panel with metrics and summary          |
| `bill-of-lading` | Bill of lading document query and management |
| `payments`       | Payment management and tracking              |
| `local-charges`  | Local charges lookup                         |
| `demurrage`      | Demurrage management                         |
| `warehouse`      | Warehouse management                         |
| `faq`            | Frequently asked questions                   |
| `admin`          | User administration and configuration        |

## Prerequisites

- Node.js 22+
- npm 11+
- Angular CLI 21

## Getting Started

1. **Install dependencies**:

   ```bash
   npm install
   ```

2. **Configure the API URL** in `src/environments/environment.ts`:

   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'https://localhost:7062/api'
   };
   ```

3. **Start the development server**:

   ```bash
   npm start
   ```

   The application will be available at: http://localhost:4200

## Build

| Command                | Environment |
| ---------------------- | ----------- |
| `npm run build:prod`   | Production  |
| `npm run build:staging`| Staging     |

Build artifacts are generated in the `dist/` directory.

## Project Structure

```
src/
├── app/
│   ├── core/                # Services, guards, interceptors, models
│   │   ├── guards/
│   │   ├── interceptors/
│   │   ├── models/
│   │   └── services/
│   ├── features/            # Feature modules
│   │   ├── auth/
│   │   ├── dashboard/
│   │   ├── bill-of-lading/
│   │   ├── payments/
│   │   ├── local-charges/
│   │   ├── demurrage/
│   │   ├── warehouse/
│   │   ├── faq/
│   │   └── admin/
│   └── shared/              # Shared components, directives, pipes
│       ├── components/
│       ├── directives/
│       └── pipes/
├── environments/
│   ├── environment.ts           # Development
│   ├── environment.staging.ts   # Staging
│   └── environment.prod.ts      # Production
├── assets/
└── styles/
```

## Environment Configuration

| File                          | Environment | Description                          |
| ----------------------------- | ----------- | ------------------------------------ |
| `environment.ts`              | Development | Default local configuration          |
| `environment.staging.ts`      | Staging     | Test environment configuration       |
| `environment.prod.ts`         | Production  | Production configuration             |

Each environment file defines at least:

```typescript
export const environment = {
  production: boolean,
  apiUrl: string
};
```

## Coding Conventions

- **Signals** for reactive state management
- **Computed** for derived state
- **takeUntilDestroyed** for managing subscriptions and preventing memory leaks
- **Reactive Forms** for all forms
- **Standalone components** instead of NgModules
- File names in kebab-case: `feature-name.component.ts`
- One component per file
