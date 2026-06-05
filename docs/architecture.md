# Architecture

## Overview

Konfigo is composed of three layers: a server backend, a web UI, and per-language client SDKs. All runtime communication between SDKs and the backend goes over gRPC.

```
┌─────────────────────────────────────┐
│           Web UI (SvelteKit)         │
│  Services · Versions · Entries       │
└──────────────────┬──────────────────┘
                   │ REST API
┌──────────────────▼──────────────────┐
│         Backend (ASP.NET Core)       │
│  REST  │  gRPC  │  SignalR           │
│─────────────────────────────────────│
│   Application  │  Domain            │
│   Infrastructure (EF Core / Dapper) │
└───────┬─────────────────────────────┘
        │           │
  PostgreSQL      Redis
  (persistence)  (outbox / pub-sub)
        ▲
        │ gRPC
┌───────┴──────────────────────────────┐
│  Client SDK (.NET / Go / Python)      │
│  Snapshot + live-update subscription  │
└───────────────────────────────────────┘
```

## Backend

### Project structure

```
apps/backend/src/
├── Konfigo               # ASP.NET Core host (controllers, gRPC, SignalR, auth)
├── Konfigo.Application   # Business logic, service interfaces, repository contracts
├── Konfigo.Domain        # Entities, value types, enums
└── Konfigo.Infrastructure# EF Core DbContext, repositories, migrations, outbox
```

The backend follows a layered architecture:

- **Domain** — pure entities and value types with no external dependencies.
- **Application** — service implementations, audit decorators, tracking decorators, and outbox-based notification.
- **Infrastructure** — EF Core (PostgreSQL) for persistence, Dapper for read queries, FluentMigrator for schema migrations.
- **Host** — DI composition root, middleware, controllers, gRPC service, SignalR hub.

### Domain model

```
ApplicationService (1) ──< ConfigVersion (1) ──< ConfigEntry
```

| Entity | Key fields |
|--------|-----------|
| `ApplicationService` | `Id (ServiceId)`, `Name`, `Description`, `RepositoryUrl`, `GitLabProjectId`, `ContactEmail` |
| `ConfigVersion` | `Id (VersionId)`, `ServiceId`, `VersionLabel`, `Description` |
| `ConfigEntry` | `Id (EntryId)`, `VersionId`, `Key`, `Name`, `RawValue`, `ValueType`, `Generation`, `GroupName` |

**Generation** is a monotonically increasing integer on each `ConfigEntry`. Every write increments it. The SDK rejects updates whose generation is ≤ the currently held value, providing last-write-wins semantics without timestamps as the primary guard.

**AuditLog** records every mutating operation (created/updated/deleted) on services, versions, and entries together with the acting `UserId`.

### Real-time delivery

Config changes flow to connected SDKs through an in-process **outbox** pattern:

1. `ConfigEntryService.SetAsync` saves the new value and emits a `ChangeEvent` to `UpdaterService`.
2. `UpdaterService` fans the event out to all active `Subscriber` instances (one per gRPC stream).
3. Each `Subscriber` yields the event to the gRPC `StartSubscribe` stream handler, which writes it to the client.
4. The `GrpcConfigChangeNotifier` additionally handles cross-instance delivery via Redis pub-sub when multiple backend replicas are deployed.

### Authentication

The authentication provider is selected at runtime via `appsettings.json`:

| Provider | Config section | Notes |
|----------|---------------|-------|
| `Saml` (default) | `Authentication.Saml` | Uses `ITfoxtec.Identity.Saml2` |
| `OpenId` | `Authentication.OpenId` | Standard OIDC code flow |
| `Jwt` | `Authentication.Jwt` | Bearer token validation |

### Authorization

Two policies are evaluated against the roles extracted from the identity provider:

| Policy constant | Default roles | Grants |
|----------------|--------------|--------|
| `canAll` | `admin` | Full CRUD on services, versions, entries |
| `canChange` | `developer` + `admin` | Read all; set entry values |

Roles are fully configurable under `Authorization.Policies` in `appsettings.json`.

## Frontend

A SvelteKit single-page app that talks to the REST API with a cookie-based session (SAML/OIDC redirect flow handled server-side by the backend).

Key routes:

| Route | Purpose |
|-------|---------|
| `/login` | Auth entry point |
| `/services` | Browse and search registered services |
| `/services/new` | Create a service (admin only) |
| `/services/:id` | Service detail — versions list |
| `/services/:id/versions/:versionId` | Config entries for a version |

The UI distinguishes between `canAll` (admin) and `canChange` (developer) users and hides mutating actions accordingly.

## Client SDKs

All three SDKs implement the same conceptual flow:

```
DiscoverDefinitions(annotated classes)
        │
        ▼
EnsureVersion(serviceId, versionLabel, schema)
  ├─ IsVersionExists?  →  reuse existing versionId
  └─ CreateVersion     →  auto-generate entries from schema
        │
        ▼
GetConfig(serviceId, versionLabel)  →  initial snapshot
        │
        ▼
Store.Update(snapshot)
        │
        ▼
StartSubscribe(serviceId, versionId, timestamp)  (long-lived stream)
        │ on each event
        ▼
Store.Update(entries, notify=true)
  └─ fire callbacks  →  BindConfig  →  options re-hydrated
```

The **Store** is a thread-safe in-memory map of `key → *string` together with per-key generation counters. `BindConfig` / `bind_config` uses reflection to map string values back to typed fields, parsing booleans, numbers, durations, datetimes, JSON, and enums.

## gRPC protocol

All three SDKs share the same `service.proto`. See [grpc.md](grpc.md) for the full message reference.

## Infrastructure requirements

| Service | Version | Purpose |
|---------|---------|---------|
| PostgreSQL | 14+ | Persistent storage |
| Redis | 7+ | Real-time event fan-out across replicas |
