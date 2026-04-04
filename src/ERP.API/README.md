# ERPv2 Backend (ASP.NET Core API)

## Overview
ERPv2 backend is a multi-tenant ERP API built on .NET 10 and ASP.NET Core.
It powers authentication, tenant isolation, POS, inventory, accounting, invoicing, reporting, email workflows, and platform administration.

This project follows a Clean Architecture layout:
- `ERP.API`: HTTP layer, middleware, controllers, contracts, runtime hosting
- `ERP.Application`: CQRS handlers, validation, business use cases
- `ERP.Domain`: entities, enums, constants, core rules
- `ERP.Infrastructure`: EF Core, repositories, JWT, external integrations

## Tech Stack
- .NET 10
- ASP.NET Core Web API (controller-based)
- Entity Framework Core (SQL Server and SQLite support)
- MediatR + CQRS
- FluentValidation
- JWT authentication
- Swagger / OpenAPI

## Run Locally
```bash
dotnet restore
dotnet run --project src/ERP.API/ERP.API.csproj --launch-profile http
```

Default endpoints:
- Swagger: `http://localhost:5058/swagger`
- Health: `http://localhost:5058/health`

## Configuration
Main configuration files:
- `src/ERP.API/appsettings.json`
- `src/ERP.API/appsettings.Development.json`
- `src/ERP.API/appsettings.Offline.json`

Common environment variables:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Key`
- `Security__EnforceAuthorization`
- `TenantResolution__EnableDevelopmentFallback`
- `EmailVerification__TokenTtlHours`
- `EmailVerification__ResendCooldownSeconds`
- `EmailVerification__DailyResendLimit`
- `EmailVerification__EnforceVerifiedUsersForTenantRequests`

Optional integrations:
- `Cloudinary__*`
- `Gemini__*`
- `Claude__*`
- `Smtp__*`

## Docker and Render
This repository contains a production Dockerfile at repository root:
- `Dockerfile`

Render setup:
- Dockerfile path: `Dockerfile`
- Build context: `.`
- Required env vars: at least `Jwt__Key` and `ConnectionStrings__DefaultConnection`
- For web services, Render injects `PORT`; container startup already handles it

## Database and Migrations
Apply migrations:
```bash
dotnet tool restore
dotnet ef database update --project src/ERP.Infrastructure --startup-project src/ERP.API
```

Create a new migration:
```bash
dotnet ef migrations add <MigrationName> --project src/ERP.Infrastructure --startup-project src/ERP.API --output-dir Persistence/Migrations
```

## Security and Tenancy
- Tenant context can be resolved from JWT claim or tenant headers
- Query filters enforce tenant data isolation
- Verified email enforcement is enabled for tenant users in protected flows
- Soft delete is used across entities for recoverability and auditing

## Testing
Run integration tests:
```bash
dotnet test tests/ERP.API.IntegrationTests/ERP.API.IntegrationTests.csproj
```

## Notes
- SQL Server LocalDB is suitable for local development on Windows
- For Linux hosting (including Render), use managed SQL Server/PostgreSQL or SQLite depending on your persistence needs
