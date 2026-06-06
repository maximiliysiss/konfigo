# Deployment

## Local development

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker (for PostgreSQL and Redis)

### 1. Start infrastructure

```bash
cd apps/backend
docker compose up -d
```

This starts PostgreSQL 14 on `:5432` and Redis 7 on `:6379`.

### 2. Configure the backend

Create `apps/backend/src/Konfigo/appsettings.Local.json`:

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=konfigo;Username=postgres;Password=pwd;Persist Security Info=True;",
    "Redis": "localhost:6379"
  },
  "Authentication": {
    "Provider": "Jwt",
    "Jwt": {
      "Authority": "http://localhost:5000",
      "Audience": "konfigo",
      "RequireHttpsMetadata": false
    }
  }
}
```

For development without a real identity provider, use `Provider: "Jwt"` and issue tokens manually, or set up a local Keycloak/Auth0 instance.

### 3. Run the backend

```bash
cd apps/backend
dotnet run --project src/Konfigo
```

The backend starts on `http://localhost:8080`. Database migrations run automatically on startup.

### 4. Run the frontend

```bash
cd apps/frontend
cp .env.example .env   # set VITE_API_PROXY_TARGET if backend is not on http://localhost:8080
npm install
npm run dev
```

The dev server starts on `http://localhost:5173`.

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
docker build --build-arg KONFIGO_VERSION=0.0.1 -t konfigo-backend:0.0.1 .
docker run -p 8080:8080 \
  -e ConnectionStrings__Postgres="Host=db;..." \
  -e ConnectionStrings__Redis="redis:6379" \
  -e Authentication__Provider="Jwt" \
  -e Authentication__Jwt__Authority="https://your-idp" \
  -e Authentication__Jwt__Audience="konfigo" \
  konfigo-backend:0.0.1
```

### Frontend

```bash
cd apps/frontend
docker build --build-arg KONFIGO_VERSION=0.0.1 -t konfigo-frontend:0.0.1 .
docker run -p 3000:3000 \
  -e PUBLIC_API_URL="https://your-backend" \
  konfigo-frontend:0.0.1
```

### Full stack with Docker Compose

The repository includes `apps/backend/docker-compose.yml` for local PostgreSQL and Redis only.
To build and run the backend, frontend, PostgreSQL, Redis, and a reverse proxy together, create a
root-level `docker-compose.yaml` like this:

```yaml
name: konfigo

services:
  postgres:
    image: postgres:14
    environment:
      POSTGRES_DB: konfigo
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: pwd
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d konfigo"]
      interval: 5s
      timeout: 5s
      retries: 20

  redis:
    image: redis:7.0.6-alpine
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 5s
      retries: 20

  backend:
    build:
      context: ./apps/backend
      args:
        KONFIGO_VERSION: 0.0.1
    environment:
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Postgres: Host=postgres;Port=5432;Database=konfigo;Username=postgres;Password=pwd;Persist Security Info=True;
      ConnectionStrings__Redis: redis:6379
      Authentication__Provider: OpenId
      Authentication__OpenId__Authority: https://your-idp.example.com
      Authentication__OpenId__ClientId: konfigo
      Authentication__OpenId__ClientSecret: change-me
      Authorization__Policies__canAll__0: admin
      Authorization__Policies__canChange__0: developer
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy

  frontend:
    build:
      context: ./apps/frontend
      args:
        KONFIGO_VERSION: 0.0.1
    environment:
      NODE_ENV: production
    depends_on:
      - backend

  proxy:
    image: nginx:1.27-alpine
    ports:
      - "8080:80"
    volumes:
      - ./nginx.conf:/etc/nginx/conf.d/default.conf:ro
    depends_on:
      - backend
      - frontend

volumes:
  postgres-data:
```

Create `nginx.conf` next to that compose file:

```nginx
server {
    listen 80;
    http2 on;

    location /api/ {
        proxy_pass http://backend:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
    }

    location /auth/ {
        proxy_pass http://backend:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
    }

    location /hubs/ {
        proxy_pass http://backend:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
    }

    location /konfigo.client.RealtimeConfigGrpcService/ {
        grpc_pass grpc://backend:8080;
    }

    location / {
        proxy_pass http://frontend:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
    }
}
```

Run it from the repository root:

```bash
docker compose up --build -d
```

The web UI will be available at `http://localhost:8080`. REST API calls use
`http://localhost:8080/api/*`, SignalR uses `http://localhost:8080/hubs/config`, and SDK gRPC
clients can connect to `http://localhost:8080`.

Replace the `Authentication__OpenId__*` values with your identity provider settings before using
this outside a local smoke test.

## Environment variables

### Backend

All settings follow ASP.NET Core's double-underscore (`__`) convention for nested keys.

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__Postgres` | PostgreSQL connection string | `Host=db;Database=konfigo;...` |
| `ConnectionStrings__Redis` | Redis connection string | `redis:6379` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production`, `Testing` |
| `Authentication__Provider` | Auth provider | `Saml`, `OpenId`, `Jwt` |
| `Authentication__Saml__MetadataLocation` | SAML IdP metadata URL or file path | `https://idp.example.com/metadata` |
| `Authentication__Saml__IdentityProviderEntityId` | SAML IdP entity ID | `https://idp.example.com` |
| `Authentication__OpenId__Authority` | OIDC authority | `https://auth.example.com` |
| `Authentication__OpenId__ClientId` | OIDC client ID | `konfigo` |
| `Authentication__OpenId__ClientSecret` | OIDC client secret | — |
| `Authentication__Jwt__Authority` | JWT authority | `https://auth.example.com` |
| `Authentication__Jwt__Audience` | JWT audience | `konfigo` |
| `Authorization__Policies__canAll__0` | Roles with full admin access | `admin` |
| `Authorization__Policies__canChange__0` | Roles with developer access | `developer` |

### Frontend

| Variable | Description |
|----------|-------------|
| `PUBLIC_API_URL` | Base URL of the Konfigo backend |
| `PUBLIC_SIGNALR_URL` | Base URL of the SignalR config hub |
| `VITE_API_PROXY_TARGET` | Backend target used by the Vite dev proxy |
| `VITE_API_PROXY_PREFIX` | Request prefix handled by the Vite dev proxy |
| `VITE_API_PROXY_REWRITE_TO` | Backend path prefix used when proxying API requests |

## Authentication setup

### SAML 2.0 (default)

```json
{
  "Authentication": {
    "Provider": "Saml",
    "Saml": {
      "ServiceProviderEntityId": "konfigo",
      "IdentityProviderEntityId": "https://your-idp.example.com",
      "MetadataLocation": "https://your-idp.example.com/saml/metadata",
      "LoadMetadata": true
    }
  }
}
```

### OpenID Connect

```json
{
  "Authentication": {
    "Provider": "OpenId",
    "OpenId": {
      "Authority": "https://your-idp.example.com",
      "ClientId": "konfigo",
      "ClientSecret": "secret",
      "ResponseType": "code",
      "Scopes": ["openid", "profile", "email"]
    }
  }
}
```

### JWT Bearer

Suitable for service-to-service access or environments with an external auth proxy:

```json
{
  "Authentication": {
    "Provider": "Jwt",
    "Jwt": {
      "Authority": "https://your-idp.example.com",
      "Audience": "konfigo",
      "RequireHttpsMetadata": true
    }
  }
}
```

## Authorization configuration

Role names come from the `role` claim in the identity token. Map them to Konfigo policies:

```json
{
  "Authorization": {
    "Policies": {
      "canAll": ["admin", "konfigo-admin"],
      "canChange": ["developer", "konfigo-dev"]
    }
  }
}
```

`canChange` automatically inherits all roles from `canAll`.

## Database migrations

Migrations run automatically on application startup via FluentMigrator. No manual `dotnet ef` commands are needed. To run them in a separate step (e.g. in a Kubernetes init container):

```bash
dotnet run --project src/Konfigo -- --migrate-only
```

(Implement the `--migrate-only` flag if needed; by default migrations run inline.)

## CI/CD

See [`.github/workflows/ci.yml`](../.github/workflows/ci.yml). The pipeline:

1. Runs all test suites in parallel on every push and pull request.
2. On a GitHub **release**, publishes:
   - Go package → `proxy.golang.org`
   - Python package → PyPI (requires `PYPI_TOKEN` secret)
   - .NET packages → NuGet.org (requires `NUGET_API` secret)
   - Docker images → DockerHub + GHCR (requires `DOCKERHUB_USERNAME` / `DOCKERHUB_TOKEN` secrets)
