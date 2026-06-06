# Konfigo

[![CI/CD](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml/badge.svg)](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-0.0.1-blue)](VERSION)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](apps/backend/Directory.Build.props)
[![Go](https://img.shields.io/badge/Go-1.25-00ADD8?logo=go)](packages/go/go.mod)
[![Python](https://img.shields.io/badge/Python-%3E%3D3.11-3776AB?logo=python&logoColor=white)](packages/python/pyproject.toml)
[![Svelte](https://img.shields.io/badge/Svelte-5-FF3E00?logo=svelte&logoColor=white)](apps/frontend/package.json)

Konfigo is a centralized, real-time configuration management service. It lets you declare typed configuration schemas in your application code, push them to a shared backend, and receive live value updates over gRPC — without restarting services.

## How it works

1. Your service declares configuration classes using language-native annotations (attributes in .NET/C#, struct tags in Go, decorators in Python).
2. On startup the SDK scans those classes, registers the schema with the Konfigo backend under a versioned label, and loads the current snapshot.
3. A long-lived gRPC stream delivers subsequent changes instantly as operators update values in the web UI.
4. Each config key tracks a **generation counter**. Stale or duplicate updates are dropped; only newer generations are applied to the in-memory store.

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
└── docs/                 # Extended documentation & wiki
```

## Tech stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core (.NET 10), PostgreSQL 14, Redis 7 |
| Frontend | SvelteKit, Tailwind CSS |
| Transport | gRPC (protobuf), SignalR |
| Auth | SAML 2.0 / OpenID Connect / JWT (configurable) |
| CI/CD | GitHub Actions → DockerHub / GHCR / NuGet / PyPI |

## Versioning

The current cross-project release version is stored in [`VERSION`](VERSION) and is `0.0.1`.
The same version is used for the backend application, .NET SDK packages, Python SDK package,
frontend package metadata, and Docker image labels. Release tags must match this value as
`0.0.1` or `v0.0.1`.
Run `python3 scripts/validate-version.py` to check all duplicated version metadata; CI runs
the same validation and fails if any copy is out of sync.

Go SDK versions are resolved by the Go module ecosystem from git tags. Use the same release
number for Go module tags when publishing the SDK.

## Quick start

### Run with Docker Compose

```bash
# Start PostgreSQL, Redis, local Dex auth, and local nginx
cd apps/backend
docker compose up -d
```

Then run the backend and frontend locally:

```bash
dotnet run --project src/Konfigo

cd ../frontend
npm install
npm run dev
```

Open `http://localhost:3000` with plain HTTP. Docker Compose starts nginx and
dependencies only; keep the host `dotnet run` and `npm run dev` processes running.

Local users:

| User | Password | Roles | Permissions |
|------|----------|-------|-------------|
| `admin@konfigo.local` | `admin` | `admin`, `developer` | `canAll`, `canChange` |
| `developer@konfigo.local` | `developer` | `developer` | `canChange` |

The backend listens on `http://localhost:8080`, the frontend dev server on
`http://localhost:5173`, Dex through nginx on `http://localhost:3000/dex`, and nginx on
`http://localhost:3000`. Use the nginx URL for the web UI so frontend, REST, auth,
and SignalR requests share one browser origin.

### Docker images (production)

Pre-built images are published to DockerHub and GHCR on every GitHub release:

```
<DOCKERHUB_USERNAME>/konfigo-backend:latest
<DOCKERHUB_USERNAME>/konfigo-backend:0.0.1
<DOCKERHUB_USERNAME>/konfigo-frontend:latest
<DOCKERHUB_USERNAME>/konfigo-frontend:0.0.1
ghcr.io/<OWNER>/konfigo-backend:latest
ghcr.io/<OWNER>/konfigo-backend:0.0.1
ghcr.io/<OWNER>/konfigo-frontend:latest
ghcr.io/<OWNER>/konfigo-frontend:0.0.1
```

## SDK quick start

Pick the SDK for your language:

### .NET

```csharp
// Program.cs
builder.Configuration.AddRealtimeConfig();
builder.Services.AddRealtimeConfig();

// Options class
[ConfigGroup(key: "Payments", groupName: "Payments")]
public sealed class PaymentsOptions
{
    [ConfigKey(description: "Payment provider", defaultValue: "Stripe")]
    public string Provider { get; set; } = string.Empty;
}

// Usage
var payments = app.Services.GetRequiredService<IOptions<PaymentsOptions>>().Value;
```

Full guide: [packages/dotnet/README.md](packages/dotnet/README.md)

### Go

```go
definitions, _ := konfigo.DiscoverDefinitions(PaymentsOptions{})
client, _ := konfigo.NewClientFromRemote(ctx, konfigo.RealtimeConfigOptions{
    IsEnabled: true,
    ServiceID: "orders",
    Version:   "1.0.0",
}, transport, definitions)

go client.Watch(ctx)

var payments PaymentsOptions
konfigo.BindConfig(client.Store.Snapshot(), &payments)
```

Full guide: [packages/go/README.md](packages/go/README.md)

### Python

```python
@config_group(key="Payments", group_name="Payments")
@dataclass
class PaymentsOptions:
    provider: str = config_key(default_value="Stripe")

client = await KonfigoClient.create(options=options, transport=transport,
                                    definitions=discover_definitions(PaymentsOptions))
client.start_background_task()
payments = bind_config(client.store.snapshot(), PaymentsOptions)
```

Full guide: [packages/python/README.md](packages/python/README.md)

## Documentation

| Topic | File |
|-------|------|
| Architecture & data model | [docs/architecture.md](docs/architecture.md) |
| Deployment & configuration | [docs/deployment.md](docs/deployment.md) |
| REST API reference | [docs/api.md](docs/api.md) |
| gRPC protocol | [docs/grpc.md](docs/grpc.md) |

## Authorization

The backend supports two policy levels configured in `appsettings.json`:

| Policy | Default role | Permissions |
|--------|-------------|-------------|
| `canAll` | `admin` | Create/update/delete services, versions, entries |
| `canChange` | `developer` | Read services and versions; set entry values |

Roles come from the identity provider claims and are fully configurable.

## CI

Every pull request runs:

- `test-go` — Go package tests
- `test-python` — Python package tests (pytest)
- `test-dotnet-package` — .NET SDK unit + integration tests
- `test-backend` — Backend tests (needs PostgreSQL + Redis)

On a GitHub **release**, all four test jobs must pass before packages are published and Docker images are pushed.
