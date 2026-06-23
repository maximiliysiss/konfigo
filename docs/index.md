# Konfigo Wiki

[![CI/CD](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml/badge.svg)](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-0.0.4-blue)](https://github.com/maximiliysiss/konfigo/blob/master/VERSION)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](https://github.com/maximiliysiss/konfigo/blob/master/LICENSE)

Konfigo is a centralized, real-time configuration management service. It lets you declare
typed configuration schemas in your application code, push them to a shared backend, and
receive live value updates over gRPC — without restarting services.

## How it works

1. Your service declares configuration classes using language-native annotations (attributes
   in .NET/C#, struct tags in Go, decorators in Python).
2. On startup the SDK scans those classes, registers the schema with the Konfigo backend
   under a versioned label, and loads the current snapshot.
3. A long-lived gRPC stream delivers subsequent changes instantly as operators update values
   in the web UI.
4. Each config key tracks a **generation counter**. Stale or duplicate updates are dropped;
   only newer generations are applied to the in-memory store.

## Where to start

<div class="grid cards" markdown>

- :material-sitemap: **[Architecture](architecture.md)**

    System components, domain model, real-time delivery, authentication & authorization

- :material-rocket-launch: **[Deployment](deployment.md)**

    Local development, Docker, environment variables, and CI/CD

- :material-api: **[REST API](api.md)**

    All HTTP endpoints with request/response shapes

- :material-swap-horizontal: **[gRPC Protocol](grpc.md)**

    Protobuf service definition and RPC semantics

- :material-puzzle: **[SDKs](sdks/index.md)**

    Client libraries for .NET, Go, and Python

- :material-play-box: **[Samples](samples.md)**

    Minimal runnable apps demonstrating each SDK

</div>

## Tech stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core (.NET 10), PostgreSQL 14, Redis 7 |
| Frontend | SvelteKit, Tailwind CSS |
| Transport | gRPC (protobuf), SignalR |
| Auth | SAML 2.0 / OpenID Connect / JWT (configurable) |
| CI/CD | GitHub Actions → DockerHub / GHCR / NuGet / PyPI |

## Repository layout

```
konfigo/
├── apps/
│   ├── backend/          # ASP.NET Core server (REST + gRPC + SignalR)
│   └── frontend/         # SvelteKit web UI
├── packages/
│   ├── dotnet/           # .NET client SDK (NuGet)
│   ├── go/               # Go client SDK
│   └── python/           # Python client SDK (PyPI)
├── samples/              # Minimal runnable apps demonstrating each SDK
│   ├── dotnet/           # ASP.NET Core sample using Konfigo.Client
│   ├── go/               # Go sample using the Go SDK + gRPC transport
│   └── python/           # Async Python sample using the Python SDK + gRPC transport
└── docs/                 # Extended documentation & wiki
```

## Quick links

- [Project repository](https://github.com/maximiliysiss/konfigo)
- [Backend source](https://github.com/maximiliysiss/konfigo/tree/master/apps/backend/src)
- [Frontend source](https://github.com/maximiliysiss/konfigo/tree/master/apps/frontend/src)
- [CI workflow](https://github.com/maximiliysiss/konfigo/blob/master/.github/workflows/ci.yml)
