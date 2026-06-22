# Authentication & Authorization

Konfigo supports three authentication providers: **OpenID Connect**, **JWT Bearer**, and **SAML 2.0**.
The provider is selected by a single `Authentication.Provider` key — everything else is read from
the corresponding configuration subsection.

---

## How provider selection works

On startup the backend reads `Authentication.Provider` and registers one ASP.NET Core scheme:

| Value | Scheme | Session storage |
|-------|--------|-----------------|
| `OpenId` | OpenID Connect (cookie) | Cookie |
| `Jwt` | JWT Bearer | Stateless (token in `Authorization` header) |
| `Saml` | SAML 2.0 (cookie) | Cookie |

> If the key is not set, `Saml` is used (the hardcoded default in code).

---

## Common settings

These fields live directly under the `Authentication` section and apply to every provider:

```json
{
  "Authentication": {
    "Provider": "OpenId",
    "EmailClaimType": "email",
    "RoleClaimType": "groups",
    "IdClaimType": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
  }
}
```

| Field | Default | Description |
|-------|---------|-------------|
| `Provider` | `Saml` | Active provider: `OpenId`, `Jwt`, or `Saml` |
| `EmailClaimType` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` | Claim used to read the user's e-mail address |
| `RoleClaimType` | `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | Claim used to read roles. With Dex or SAML this is typically `groups` |
| `IdClaimType` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | Claim used as the unique user identifier |

---

## OpenID Connect

Best choice for modern providers (Keycloak, Auth0, Dex, Azure AD, Google, Okta, etc.).
Uses Authorization Code Flow with a server-side cookie session.

### Configuration

```json
{
  "Authentication": {
    "Provider": "OpenId",
    "RoleClaimType": "groups",
    "OpenId": {
      "Authority": "https://your-idp.example.com",
      "ClientId": "konfigo",
      "ClientSecret": "your-client-secret",
      "RequireHttpsMetadata": true,
      "ResponseType": "code",
      "Scope": [
        "openid",
        "profile",
        "email",
        "groups"
      ]
    }
  }
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `Authority` | Yes | OIDC provider URL. The backend fetches `{Authority}/.well-known/openid-configuration` for autodiscovery |
| `ClientId` | Yes | Client ID registered with the provider |
| `ClientSecret` | Yes | Client secret |
| `RequireHttpsMetadata` | — | Set to `false` to allow HTTP for autodiscovery (local development only) |
| `ResponseType` | — | `code` — Authorization Code Flow (recommended) |
| `Scope` | — | Requested scopes. Add any provider-specific scope that carries roles (e.g. `groups`) |

### Local development with Dex

`docker-compose.yml` includes [Dex](https://dexidp.io/) — a lightweight OIDC provider.
Its configuration lives in `apps/backend/docker/dex/config.yaml`.

Start the infrastructure:

```bash
cd apps/backend
docker compose up -d
```

Configure `appsettings.Local.json`:

```json
{
  "Authentication": {
    "Provider": "OpenId",
    "RoleClaimType": "groups",
    "OpenId": {
      "Authority": "http://localhost:3000/dex",
      "ClientId": "konfigo",
      "ClientSecret": "konfigo-local-secret",
      "RequireHttpsMetadata": false,
      "ResponseType": "code",
      "Scope": ["openid", "profile", "email", "groups"]
    }
  }
}
```

Dex issues a `groups` claim with values from `docker/dex/config.yaml`:

```yaml
staticPasswords:
  - email: admin@konfigo.local
    username: admin
    groups:
      - admin
      - developer
  - email: developer@konfigo.local
    username: developer
    groups:
      - developer
```

This is why `RoleClaimType` must be set to `groups` — that is the claim Dex puts roles into.

---

## JWT Bearer

Used when the frontend (or another service) obtains a JWT token from the provider independently
and passes it in the `Authorization: Bearer <token>` header. No server-side session is created —
every request is validated against the token.

Suitable for:

- Machine-to-machine (service-to-service) access
- Environments with an external auth proxy (e.g. Nginx `auth_request`)
- SPAs using OAuth2 PKCE where the token is stored on the client

### Configuration

```json
{
  "Authentication": {
    "Provider": "Jwt",
    "RoleClaimType": "groups",
    "Jwt": {
      "Authority": "https://your-idp.example.com",
      "Audience": "konfigo",
      "RequireHttpsMetadata": true,
      "TokenValidationParameters": {
        "ValidateAudience": true,
        "ValidateIssuer": true
      }
    }
  }
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `Authority` | Yes | Provider URL used to load JWKS signing keys via `{Authority}/.well-known/openid-configuration` |
| `Audience` | Yes | Expected `aud` value in the token — must match what the provider issues |
| `RequireHttpsMetadata` | — | Set to `false` to allow HTTP (development only) |
| `TokenValidationParameters` | — | Extra validation options from `Microsoft.IdentityModel.Tokens.TokenValidationParameters` |

You can also supply fields that the frontend uses for its OAuth2 login flow — they are exposed
through `GET /auth/config`:

```json
{
  "Authentication": {
    "Jwt": {
      "Authority": "https://your-idp.example.com",
      "ClientId": "konfigo",
      "AuthorizeUrl": "https://your-idp.example.com/oauth2/authorize",
      "TokenUrl": "https://your-idp.example.com/oauth2/token",
      "Scopes": "openid profile email",
      "Audience": "konfigo"
    }
  }
}
```

| Field | Description |
|-------|-------------|
| `ClientId` | Client ID — forwarded to the frontend for the OAuth2 flow |
| `AuthorizeUrl` | Authorization endpoint URL. Needed when the provider does not support autodiscovery or uses a non-standard path |
| `TokenUrl` | Token exchange endpoint URL |
| `Scopes` | Space-separated scope string |

### Local development with mock-oauth2

`docker-compose.yml` includes [mock-oauth2-server](https://github.com/navikt/mock-oauth2-server) —
an OAuth2 server with an interactive login form. Configuration: `apps/backend/docker/mock-oauth2/config.json`.

```json
{
  "Authentication": {
    "Provider": "Jwt",
    "RoleClaimType": "groups",
    "Jwt": {
      "Authority": "http://localhost:8082/default",
      "ClientId": "konfigo",
      "AuthorizeUrl": "http://localhost:3000/mock-oauth2/default/authorize",
      "TokenUrl": "http://localhost:3000/mock-oauth2/default/token",
      "Scopes": "openid profile email",
      "Audience": "konfigo",
      "RequireHttpsMetadata": false,
      "TokenValidationParameters": {
        "ValidateAudience": false,
        "ValidateIssuer": false
      }
    }
  }
}
```

Users are defined in `docker/mock-oauth2/config.json` via `requestMappings` — the server matches
the login username and injects a pre-configured set of claims, including `groups`, into the issued token.

---

## SAML 2.0

The default provider. Implemented via [Sustainsys.Saml2](https://saml2.sustainsys.com/).
Uses a cookie session. The backend acts as Service Provider (SP); the Identity Provider (IdP)
is configured through a metadata URL.

### Configuration

```json
{
  "Authentication": {
    "Provider": "Saml",
    "EmailClaimType": "mail",
    "RoleClaimType": "groups",
    "Saml": {
      "SpOptionsEntityId": "https://your-app.example.com/saml2/Metadata",
      "SpOptionsModulePath": "/saml2",
      "IdentityProviderEntityId": "https://your-idp.example.com/saml2/idp/metadata",
      "IdentityProviderMetadataUrl": "https://your-idp.example.com/saml2/idp/metadata"
    }
  }
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `SpOptionsEntityId` | Yes | EntityID of your SP — a unique URI that identifies the application to the IdP. Typically the URL of the SP metadata endpoint |
| `SpOptionsModulePath` | Yes | Path prefix for SAML SP endpoints (ACS, SLO, Metadata). Defaults to `/saml2` |
| `IdentityProviderEntityId` | Yes | EntityID of the IdP as it appears in its metadata. Must match exactly — responses with a different entity ID are rejected |
| `IdentityProviderMetadataUrl` | Yes | URL from which the backend downloads IdP XML metadata on startup (`LoadMetadata: true`) |

> **SAML endpoint routing.** `SpOptionsModulePath` sets the prefix for paths that Sustainsys
> registers automatically: `{ModulePath}/Acs`, `{ModulePath}/Metadata`, `{ModulePath}/Logout`.
> When running behind nginx, make sure these paths are proxied to the backend and not to the IdP.

### Local development with SimpleSAMLphp

`docker-compose.yml` includes a [SimpleSAMLphp IdP](https://simplesamlphp.org/). User accounts
are defined in `apps/backend/docker/saml-config/authsources.php`.

```json
{
  "Authentication": {
    "Provider": "Saml",
    "EmailClaimType": "mail",
    "RoleClaimType": "groups",
    "Saml": {
      "SpOptionsEntityId": "http://localhost:3000/saml2/Metadata",
      "SpOptionsModulePath": "/saml2",
      "IdentityProviderEntityId": "http://localhost:3000/saml2/simplesaml/saml2/idp/metadata.php",
      "IdentityProviderMetadataUrl": "http://localhost:3000/saml2/simplesaml/saml2/idp/metadata.php"
    }
  }
}
```

SimpleSAMLphp sends the `mail` attribute as the e-mail and `groups` as roles — hence
`EmailClaimType: "mail"`.

Nginx routes traffic as follows (from `docker/nginx/default.conf`):

```nginx
location /saml2/simplesaml/ {
    proxy_pass http://saml-idp:8080/simplesaml/;   # IdP traffic
}

location /saml2/ {
    proxy_pass http://host.docker.internal:8080;    # SP endpoints to the backend
}
```

---

## Authorization

Regardless of the provider, authorization works by mapping roles from identity claims to Konfigo policies.

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

| Policy | Access |
|--------|--------|
| `canAll` | Full administrative access: managing services, keys, and settings |
| `canChange` | Editing configuration values. Automatically includes all roles from `canAll` |

Roles are looked up across the user's claims. The backend checks several well-known claim types —
`role`, `roles`, `groups`, `name` — as well as the type specified in `RoleClaimType`.

To inspect which claims and roles were received from the provider, call `GET /auth/me` after login.
It returns the current user's id, email, name, full role set, and active policy list.

---

## Switching providers in local development

All three providers start simultaneously with `docker compose up -d`. To switch, change
`Authentication.Provider` in `appsettings.Local.json` and restart the backend:

=== "OpenID Connect (Dex)"

    ```json
    {
      "Authentication": {
        "Provider": "OpenId",
        "RoleClaimType": "groups",
        "OpenId": {
          "Authority": "http://localhost:3000/dex",
          "ClientId": "konfigo",
          "ClientSecret": "konfigo-local-secret",
          "RequireHttpsMetadata": false,
          "ResponseType": "code",
          "Scope": ["openid", "profile", "email", "groups"]
        }
      }
    }
    ```

=== "JWT Bearer (mock-oauth2)"

    ```json
    {
      "Authentication": {
        "Provider": "Jwt",
        "RoleClaimType": "groups",
        "Jwt": {
          "Authority": "http://localhost:8082/default",
          "ClientId": "konfigo",
          "AuthorizeUrl": "http://localhost:3000/mock-oauth2/default/authorize",
          "TokenUrl": "http://localhost:3000/mock-oauth2/default/token",
          "Scopes": "openid profile email",
          "Audience": "konfigo",
          "RequireHttpsMetadata": false,
          "TokenValidationParameters": {
            "ValidateAudience": false,
            "ValidateIssuer": false
          }
        }
      }
    }
    ```

=== "SAML 2.0 (SimpleSAMLphp)"

    ```json
    {
      "Authentication": {
        "Provider": "Saml",
        "EmailClaimType": "mail",
        "RoleClaimType": "groups",
        "Saml": {
          "SpOptionsEntityId": "http://localhost:3000/saml2/Metadata",
          "SpOptionsModulePath": "/saml2",
          "IdentityProviderEntityId": "http://localhost:3000/saml2/simplesaml/saml2/idp/metadata.php",
          "IdentityProviderMetadataUrl": "http://localhost:3000/saml2/simplesaml/saml2/idp/metadata.php"
        }
      }
    }
    ```

Test users are the same across all three providers:

| User | Password | Roles | Konfigo policies |
|------|----------|-------|-----------------|
| `admin@konfigo.local` | `admin` | `admin`, `developer` | `canAll`, `canChange` |
| `developer@konfigo.local` | `developer` | `developer` | `canChange` |

---

## Production: environment variables

All `appsettings.json` keys are available as environment variables using ASP.NET Core's
double-underscore (`__`) nesting convention:

=== "OpenID Connect"

    ```bash
    Authentication__Provider=OpenId
    Authentication__RoleClaimType=role
    Authentication__OpenId__Authority=https://your-idp.example.com
    Authentication__OpenId__ClientId=konfigo
    Authentication__OpenId__ClientSecret=your-secret
    Authentication__OpenId__RequireHttpsMetadata=true
    Authentication__OpenId__ResponseType=code
    # Array scopes via index:
    Authentication__OpenId__Scope__0=openid
    Authentication__OpenId__Scope__1=profile
    Authentication__OpenId__Scope__2=email
    Authentication__OpenId__Scope__3=roles
    ```

=== "JWT Bearer"

    ```bash
    Authentication__Provider=Jwt
    Authentication__RoleClaimType=role
    Authentication__Jwt__Authority=https://your-idp.example.com
    Authentication__Jwt__Audience=konfigo
    Authentication__Jwt__RequireHttpsMetadata=true
    # Optional — for frontend OAuth2 flow:
    Authentication__Jwt__ClientId=konfigo
    Authentication__Jwt__AuthorizeUrl=https://your-idp.example.com/oauth2/authorize
    Authentication__Jwt__TokenUrl=https://your-idp.example.com/oauth2/token
    Authentication__Jwt__Scopes=openid profile email
    ```

=== "SAML 2.0"

    ```bash
    Authentication__Provider=Saml
    Authentication__EmailClaimType=mail
    Authentication__RoleClaimType=groups
    Authentication__Saml__SpOptionsEntityId=https://your-app.example.com/saml2/Metadata
    Authentication__Saml__SpOptionsModulePath=/saml2
    Authentication__Saml__IdentityProviderEntityId=https://your-idp.example.com/saml2/idp/metadata
    Authentication__Saml__IdentityProviderMetadataUrl=https://your-idp.example.com/saml2/idp/metadata
    ```

Authorization policies via environment variables (roles use array index notation):

```bash
Authorization__Policies__canAll__0=admin
Authorization__Policies__canAll__1=konfigo-admin
Authorization__Policies__canChange__0=developer
Authorization__Policies__canChange__1=konfigo-dev
```
