# Deployment

## Local development

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker (for PostgreSQL, Redis, Dex, and local nginx)

### 1. Start infrastructure

```bash
cd apps/backend
docker compose up -d
```

This starts PostgreSQL 14 on `:5432`, Redis 7 on `:6379`, Dex through nginx on
`http://localhost:3000/dex`, and nginx on `http://localhost:3000`.

### 2. Configure the backend

`apps/backend/src/Konfigo/appsettings.Local.json` is configured for those local dependencies:

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=konfigo;Username=postgres;Password=pwd;Persist Security Info=True;",
    "Redis": "localhost:6379"
  },
  "Authentication": {
    "Provider": "OpenId",
    "RoleClaimType": "groups",
    "OpenId": {
      "Authority": "http://localhost:3000/dex",
      "ClientId": "konfigo",
      "ClientSecret": "konfigo-local-secret",
      "RequireHttpsMetadata": false,
      "ResponseType": "code",
      "Scopes": ["openid", "profile", "email", "groups"]
    }
  },
  "Authorization": {
    "Policies": {
      "canAll": ["admin"],
      "canChange": ["developer"]
    }
  }
}
```

Local Dex users:

| User | Password | Dex groups | Konfigo permissions |
|------|----------|------------|---------------------|
| `admin@konfigo.local` | `admin` | `admin`, `developer` | `canAll`, `canChange` |
| `developer@konfigo.local` | `developer` | `developer` | `canChange` |

### 3. Run the backend

```bash
cd apps/backend
dotnet run --project src/Konfigo
```

The backend starts on `http://localhost:8080`. Database migrations run automatically on startup.

### 4. Run the frontend

```bash
cd apps/frontend
cp .env.example .env
npm install
npm run dev
```

The dev server starts on `http://localhost:5173`. Open `http://localhost:3000` with plain
HTTP; nginx proxies the frontend dev server plus backend API/auth/SignalR paths through one
origin.

## Docker (production)

The repository release version is stored in `VERSION`. Docker builds accept it through
`KONFIGO_VERSION` and write it to the `org.opencontainers.image.version` label and the
`KONFIGO_VERSION` environment variable inside the image.

### Backend

```dockerfile
# apps/backend/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Konfigo/Konfigo.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Konfigo.dll"]
```

Build and run:

```bash
cd apps/backend
docker build --build-arg KONFIGO_VERSION=0.0.4 -t konfigo-backend:0.0.4 .
docker run -p 8080:8080 \
  -e ConnectionStrings__Postgres="Host=db;..." \
  -e ConnectionStrings__Redis="redis:6379" \
  -e Authentication__Provider="Jwt" \
  -e Authentication__Jwt__Authority="https://your-idp" \
  -e Authentication__Jwt__Audience="konfigo" \
  konfigo-backend:0.0.4
```

### Frontend

```bash
cd apps/frontend
docker build --build-arg KONFIGO_VERSION=0.0.4 -t konfigo-frontend:0.0.4 .
docker run -p 3000:3000 \
  -e PUBLIC_API_URL="https://your-backend" \
  konfigo-frontend:0.0.4
```

### Local dependencies with Docker Compose

The repository includes `apps/backend/docker-compose.yml` for external dependencies and the
local nginx entrypoint used by locally running backend/frontend processes:

Run it from `apps/backend`:

```bash
docker compose up -d
```

It starts PostgreSQL, Redis, Dex, and nginx. The backend runs on the host at
`http://localhost:8080`, the frontend dev server runs on `http://localhost:5173`, and nginx
serves the browser-facing local UI at `http://localhost:3000`. Keep the host backend and
frontend processes running while using the nginx URL.

## Environment variables

### Backend

All settings follow ASP.NET Core's double-underscore (`__`) convention for nested keys.

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__Postgres` | PostgreSQL connection string for persistence and Publo-backed event delivery | `Host=db;Database=konfigo;...` |
| `ConnectionStrings__Redis` | Redis connection string for distributed locks | `redis:6379` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production`, `Testing` |
| `Authentication__Provider` | Auth provider | `Saml`, `OpenId`, `Jwt` |
| `Authentication__RoleClaimType` | Claim type used for roles | `role`, `groups` |
| `Authentication__Saml__MetadataLocation` | SAML IdP metadata URL or file path | `https://idp.example.com/metadata` |
| `Authentication__Saml__IdentityProviderEntityId` | SAML IdP entity ID | `https://idp.example.com` |
| `Authentication__OpenId__Authority` | OIDC authority | `https://auth.example.com` |
| `Authentication__OpenId__ClientId` | OIDC client ID | `konfigo` |
| `Authentication__OpenId__ClientSecret` | OIDC client secret | — |
| `Authentication__OpenId__RequireHttpsMetadata` | Require HTTPS OIDC metadata | `true` |
| `Authentication__Jwt__Authority` | JWT authority | `https://auth.example.com` |
| `Authentication__Jwt__Audience` | JWT audience | `konfigo` |
| `Authorization__Policies__canAll__0` | Roles with full admin access | `admin` |
| `Authorization__Policies__canChange__0` | Roles with developer access | `developer` |

### Frontend

| Variable | Description |
|----------|-------------|
| `PUBLIC_API_URL` | Base URL of the Konfigo backend |
| `PUBLIC_SIGNALR_URL` | Base URL of the SignalR config hub |

## Authentication setup

Konfigo supports three authentication providers: OpenID Connect, JWT Bearer, and SAML 2.0.
See the **[Authentication](authentication.md)** page for the full configuration reference,
local development setup for each provider, and environment variable examples.

## Database migrations

Migrations run automatically on application startup via FluentMigrator. No manual `dotnet ef` commands are needed. To run them in a separate step (e.g. in a Kubernetes init container):

```bash
dotnet run --project src/Konfigo -- --migrate-only
```

(Implement the `--migrate-only` flag if needed; by default migrations run inline.)

## CI/CD

See [`.github/workflows/ci.yml`](https://github.com/maximiliysiss/konfigo/blob/master/.github/workflows/ci.yml). The pipeline:

1. Runs all test suites in parallel on every push and pull request.
2. On a GitHub **release**, publishes:
   - Go package → `proxy.golang.org`
   - Python package → PyPI (requires `PYPI_TOKEN` secret)
   - .NET packages → NuGet.org (requires `NUGET_API` secret)
   - Docker images → DockerHub + GHCR (requires `DOCKERHUB_USERNAME` / `DOCKERHUB_TOKEN` secrets)
