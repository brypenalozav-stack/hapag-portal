# Hapag-Lloyd Portal API (HapagPortal)

API RESTful para el portal de clientes de Hapag-Lloyd Chile/Bolivia.

## Tabla de Contenidos

- [Descripcion](#descripcion)
- [Arquitectura](#arquitectura)
- [Estructura de la Solucion](#estructura-de-la-solucion)
- [Requisitos Previos](#requisitos-previos)
- [Primeros Pasos](#primeros-pasos)
- [Endpoints de la API](#endpoints-de-la-api)
- [Pruebas](#pruebas)
- [Caracteristicas Principales](#caracteristicas-principales)
- [Configuracion](#configuracion)
- [Convenciones del Proyecto](#convenciones-del-proyecto)

## Descripcion

Backend del portal de clientes de Hapag-Lloyd para las operaciones en Chile y Bolivia. Proporciona una API RESTful que gestiona autenticacion, documentos de embarque, pagos, cargos locales, sobreestadia, bodegas, preguntas frecuentes y administracion de usuarios.

## Arquitectura

El proyecto sigue los principios de **Clean Architecture** con las siguientes tecnologias:

| Tecnologia          | Version |
| ------------------- | ------- |
| .NET                | 10      |
| Entity Framework Core | 9     |
| MediatR (CQRS)     | 12.4    |
| FluentValidation    | 11.11   |

## Estructura de la Solucion

```
backend/
├── src/
│   ├── HapagPortal.Domain/              # Entidades, enums, interfaces del dominio
│   ├── HapagPortal.Application/         # Casos de uso, CQRS commands/queries, validaciones
│   ├── HapagPortal.Infrastructure/      # Implementaciones de persistencia, servicios externos
│   ├── HapagPortal.WebApi/              # Controladores, middlewares, configuracion de la API
│   └── HapagPortal.DatabaseMigrations/  # Migraciones de base de datos
├── tests/
│   ├── HapagPortal.Domain.Tests/
│   ├── HapagPortal.Application.Tests/
│   ├── HapagPortal.Infrastructure.Tests/
│   ├── HapagPortal.WebApi.Tests/
│   └── HapagPortal.Architecture.Tests/
└── HapagPortal.sln
```

**5 proyectos fuente** + **5 proyectos de prueba**

## Requisitos Previos

- .NET 10 SDK
- SQL Server (LocalDB o instancia completa)
- Visual Studio 2022+ o VS Code

## Primeros Pasos

1. **Clonar el repositorio**

   ```bash
   git clone <url-del-repositorio>
   cd hapag/backend
   ```

2. **Configurar la cadena de conexion** en `appsettings.Development.json` o mediante user-secrets:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=HapagPortal;Trusted_Connection=True"
   ```

3. **Configurar el secreto JWT** via user-secrets:

   ```bash
   dotnet user-secrets set "Jwt:Secret" "your-secret-key-here"
   ```

4. **Ejecutar las migraciones**:

   ```bash
   dotnet run --project src/HapagPortal.DatabaseMigrations
   ```

5. **Ejecutar la API**:

   ```bash
   dotnet run --project src/HapagPortal.WebApi
   ```

   Swagger se abre automaticamente en: https://localhost:7062/swagger

## Endpoints de la API

La API cuenta con **12 controladores** y **49 endpoints** organizados de la siguiente manera:

| Controlador        | Descripcion                              | Endpoints |
| ------------------ | ---------------------------------------- | --------- |
| Auth               | Autenticacion y gestion de tokens        | ~4        |
| Users              | Administracion de usuarios               | ~5        |
| Roles              | Gestion de roles y permisos              | ~4        |
| BillOfLading       | Documentos de embarque (BL)              | ~5        |
| Payments           | Gestion de pagos                         | ~4        |
| LocalCharges       | Cargos locales                           | ~4        |
| Demurrage          | Sobreestadia                             | ~4        |
| Warehouse          | Gestion de bodegas                       | ~4        |
| FAQ                | Preguntas frecuentes                     | ~4        |
| Dashboard          | Metricas y resumen                       | ~3        |
| Files              | Carga y descarga de archivos             | ~4        |
| Configuration      | Configuracion del sistema                | ~4        |

## Pruebas

```bash
dotnet test
```

**111 pruebas** utilizando:

| Framework          | Proposito                    |
| ------------------ | ---------------------------- |
| xUnit v3           | Framework de pruebas         |
| FluentAssertions   | Aserciones legibles          |
| NSubstitute        | Mocking                      |
| NetArchTest        | Pruebas de arquitectura      |

## Caracteristicas Principales

- **Autenticacion JWT** con refresh tokens
- **API Versioning** (v1)
- **Swagger / OpenAPI** con documentacion interactiva
- **CORS** configurado para el frontend
- **Manejo global de excepciones** via middleware
- **Logging** estructurado

## Configuracion

Variables de configuracion principales (via `appsettings.json` o user-secrets):

| Clave                                | Descripcion                        |
| ------------------------------------ | ---------------------------------- |
| `ConnectionStrings:DefaultConnection`| Cadena de conexion a SQL Server    |
| `Jwt:Secret`                         | Clave secreta para firmar tokens   |
| `Jwt:Issuer`                         | Emisor del token JWT               |
| `Jwt:Audience`                       | Audiencia del token JWT            |
| `Jwt:ExpirationInMinutes`            | Tiempo de expiracion del token     |
| `Cors:AllowedOrigins`                | Origenes permitidos para CORS      |

## Convenciones del Proyecto

- **Patron CQRS**: Commands para escritura, Queries para lectura, separados via MediatR
- **Result Pattern**: Manejo de errores sin excepciones, retornando objetos `Result<T>`
- **BaseAuditableEntity**: Todas las entidades auditables heredan de esta clase base (`CreatedBy`, `CreatedAt`, `LastModifiedBy`, `LastModifiedAt`)
- **FluentValidation**: Validaciones declarativas en clases `Validator` separadas
- **Vertical Slice por feature**: Cada feature agrupa su command/query, handler y validator

---

# Hapag-Lloyd Portal API (HapagPortal)

RESTful API for the Hapag-Lloyd Chile/Bolivia customer portal.

## Table of Contents

- [Description](#description)
- [Architecture](#architecture)
- [Solution Structure](#solution-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [API Endpoints](#api-endpoints)
- [Testing](#testing)
- [Key Features](#key-features)
- [Configuration](#configuration-1)
- [Project Conventions](#project-conventions)

## Description

Backend for the Hapag-Lloyd customer portal serving operations in Chile and Bolivia. Provides a RESTful API managing authentication, bills of lading, payments, local charges, demurrage, warehouses, FAQs, and user administration.

## Architecture

The project follows **Clean Architecture** principles with the following technologies:

| Technology            | Version |
| --------------------- | ------- |
| .NET                  | 10      |
| Entity Framework Core | 9       |
| MediatR (CQRS)       | 12.4    |
| FluentValidation      | 11.11   |

## Solution Structure

```
backend/
├── src/
│   ├── HapagPortal.Domain/              # Domain entities, enums, interfaces
│   ├── HapagPortal.Application/         # Use cases, CQRS commands/queries, validations
│   ├── HapagPortal.Infrastructure/      # Persistence implementations, external services
│   ├── HapagPortal.WebApi/              # Controllers, middlewares, API configuration
│   └── HapagPortal.DatabaseMigrations/  # Database migrations
├── tests/
│   ├── HapagPortal.Domain.Tests/
│   ├── HapagPortal.Application.Tests/
│   ├── HapagPortal.Infrastructure.Tests/
│   ├── HapagPortal.WebApi.Tests/
│   └── HapagPortal.Architecture.Tests/
└── HapagPortal.sln
```

**5 source projects** + **5 test projects**

## Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022+ or VS Code

## Getting Started

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd hapag/backend
   ```

2. **Configure the connection string** in `appsettings.Development.json` or via user-secrets:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=HapagPortal;Trusted_Connection=True"
   ```

3. **Configure the JWT secret** via user-secrets:

   ```bash
   dotnet user-secrets set "Jwt:Secret" "your-secret-key-here"
   ```

4. **Run migrations**:

   ```bash
   dotnet run --project src/HapagPortal.DatabaseMigrations
   ```

5. **Run the API**:

   ```bash
   dotnet run --project src/HapagPortal.WebApi
   ```

   Swagger opens automatically at: https://localhost:7062/swagger

## API Endpoints

The API has **12 controllers** and **49 endpoints** organized as follows:

| Controller         | Description                              | Endpoints |
| ------------------ | ---------------------------------------- | --------- |
| Auth               | Authentication and token management      | ~4        |
| Users              | User administration                      | ~5        |
| Roles              | Role and permission management           | ~4        |
| BillOfLading       | Bill of lading documents                 | ~5        |
| Payments           | Payment management                       | ~4        |
| LocalCharges       | Local charges                            | ~4        |
| Demurrage          | Demurrage management                     | ~4        |
| Warehouse          | Warehouse management                     | ~4        |
| FAQ                | Frequently asked questions               | ~4        |
| Dashboard          | Metrics and summary                      | ~3        |
| Files              | File upload and download                 | ~4        |
| Configuration      | System configuration                     | ~4        |

## Testing

```bash
dotnet test
```

**111 tests** using:

| Framework          | Purpose                      |
| ------------------ | ---------------------------- |
| xUnit v3           | Test framework               |
| FluentAssertions   | Readable assertions          |
| NSubstitute        | Mocking                      |
| NetArchTest        | Architecture tests           |

## Key Features

- **JWT Authentication** with refresh tokens
- **API Versioning** (v1)
- **Swagger / OpenAPI** with interactive documentation
- **CORS** configured for the frontend
- **Global exception handling** via middleware
- **Structured logging**

## Configuration

Main configuration keys (via `appsettings.json` or user-secrets):

| Key                                  | Description                        |
| ------------------------------------ | ---------------------------------- |
| `ConnectionStrings:DefaultConnection`| SQL Server connection string       |
| `Jwt:Secret`                         | Secret key for signing tokens      |
| `Jwt:Issuer`                         | JWT token issuer                   |
| `Jwt:Audience`                       | JWT token audience                 |
| `Jwt:ExpirationInMinutes`            | Token expiration time              |
| `Cors:AllowedOrigins`                | Allowed origins for CORS           |

## Project Conventions

- **CQRS Pattern**: Commands for writes, Queries for reads, separated via MediatR
- **Result Pattern**: Error handling without exceptions, returning `Result<T>` objects
- **BaseAuditableEntity**: All auditable entities inherit from this base class (`CreatedBy`, `CreatedAt`, `LastModifiedBy`, `LastModifiedAt`)
- **FluentValidation**: Declarative validations in separate `Validator` classes
- **Vertical Slice per feature**: Each feature groups its command/query, handler, and validator
