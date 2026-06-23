# Konfigo Backend

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](Directory.Build.props)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-API-512BD4?logo=dotnet)](src/Konfigo/Konfigo.csproj)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14-4169E1?logo=postgresql&logoColor=white)](docker-compose.yml)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white)](docker-compose.yml)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](Dockerfile)
[![CI/CD](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml/badge.svg)](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml)

ASP.NET Core backend for Konfigo, a centralized real-time configuration management service. It exposes REST APIs for the web UI, gRPC streaming for SDK clients, and SignalR notifications for browser sessions.

## What This Image Contains

- Konfigo REST API for services, versions, config entries, audit logs, and authentication metadata
- gRPC realtime configuration stream for .NET, Go, and Python clients
- SignalR hub for frontend updates
- Automatic FluentMigrator database migrations on startup
- PostgreSQL persistence and Redis-backed distributed coordination
- Configurable authentication with OpenID Connect, JWT bearer, or SAML 2.0

## Run

```bash
docker run -p 8080:8080 -p 8081:8081 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__Postgres="Host=postgres;Port=5432;Database=konfigo;Username=postgres;Password=change-me;Persist Security Info=True;" \
  -e ConnectionStrings__Redis="redis:6379" \
  -e Authentication__Provider="Jwt" \
  -e Authentication__Jwt__Authority="https://your-idp.example.com" \
  -e Authentication__Jwt__Audience="konfigo" \
  your-dockerhub-user/konfigo-backend:latest
```

The HTTP REST API listens on port `8080`. The gRPC endpoint listens on port `8081` with HTTP/2.

## Required Services

| Service | Purpose |
|---------|---------|
| PostgreSQL 14+ | Application data, audit data, and event delivery state |
| Redis 7+ | Distributed locks across backend replicas |
| Identity provider | OpenID Connect, JWT bearer, or SAML 2.0 authentication |

## Important Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__Postgres` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Redis connection string |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment name |
| `Authentication__Provider` | `OpenId`, `Jwt`, or `Saml` |
| `Authentication__RoleClaimType` | Claim type used for role mapping |
| `Authorization__Policies__canAll__0` | First role allowed to administer services, versions, and entries |
| `Authorization__Policies__canChange__0` | First role allowed to read services and change config values |

## Local Development

```bash
cd apps/backend
docker compose up -d
dotnet run --project src/Konfigo
```

The local compose file starts PostgreSQL, Redis, Dex, and nginx. Open the full app through `http://localhost:3000` after also starting the frontend dev server.

## More Information

- Main repository: https://github.com/maximiliysiss/konfigo
- Full documentation: https://maximiliysiss.github.io/konfigo/
- Deployment guide: https://github.com/maximiliysiss/konfigo/blob/master/docs/deployment.md
- Authentication guide: https://github.com/maximiliysiss/konfigo/blob/master/docs/authentication.md
- REST API reference: https://github.com/maximiliysiss/konfigo/blob/master/docs/api.md
- gRPC protocol: https://github.com/maximiliysiss/konfigo/blob/master/docs/grpc.md
