# REST API Reference

All endpoints are prefixed with `/api`. Authentication is required for every endpoint. Authorization depends on the policy noted per endpoint.

## Services

### Search services

`POST /api/services/search`  
**Policy:** `canChange`

Request body:

```json
{
  "name": "orders",
  "pageSize": 12,
  "pageToken": null
}
```

Response:

```json
{
  "entities": [
    {
      "id": "uuid",
      "num": 1,
      "name": "Orders",
      "description": "...",
      "repositoryUrl": "https://github.com/org/repo",
      "contactEmail": "team@example.com"
    }
  ],
  "nextPageToken": "base64-cursor"
}
```

Pagination uses cursor-based tokens. Pass `nextPageToken` as `pageToken` to fetch the next page. An empty `nextPageToken` means there are no more pages.

### Get service by ID

`GET /api/services/{serviceId}`  
**Policy:** `canChange`

### Create service

`POST /api/services`  
**Policy:** `canAll`

Request body:

```json
{
  "name": "Orders",
  "description": "Order processing service",
  "repositoryUrl": "https://github.com/org/orders",
  "contactEmail": "orders-team@example.com"
}
```

### Update service

`PUT /api/services/{serviceId}`  
**Policy:** `canAll`

Same body shape as Create.

### Delete service

`DELETE /api/services/{serviceId}`  
**Policy:** `canAll`

---

## Config versions

### List versions for a service

`GET /api/configversions/{serviceId}`  
**Policy:** `canChange`

### Get version by ID

`GET /api/configversions/{serviceId}/{versionId}`  
**Policy:** `canChange`

### Create version

`POST /api/configversions/{serviceId}`  
**Policy:** `canAll`

Request body:

```json
{
  "versionLabel": "1.2.0",
  "description": "Initial version"
}
```

### Update version

`PUT /api/configversions/{serviceId}/{versionId}`  
**Policy:** `canAll`

### Delete version

`DELETE /api/configversions/{serviceId}/{versionId}`  
**Policy:** `canAll`

---

## Config entries

All config entry endpoints are scoped to a `serviceId` + `versionId` pair.

### List entries

`GET /api/configentries/{serviceId}/{versionId}`  
**Policy:** `canChange`

Returns an array of:

```json
{
  "id": "uuid",
  "key": "Payments:Provider",
  "name": "Provider",
  "rawValue": "Stripe",
  "valueType": 1,
  "enumDefinition": null,
  "description": "Payment provider",
  "groupName": "Payments",
  "groupDescription": null,
  "generation": 3
}
```

### Create entry

`POST /api/configentries/{serviceId}/{versionId}`  
**Policy:** `canAll`

```json
{
  "key": "Payments:Provider",
  "name": "Provider",
  "rawValue": "Stripe",
  "valueType": 1,
  "enumDefinition": null,
  "description": "Payment provider",
  "groupName": "Payments",
  "groupDescription": null
}
```

### Set entry values (batch)

`PUT /api/configentries/{serviceId}/{versionId}/set`  
**Policy:** `canChange`

Sets raw values for one or more existing entries. Each item must include the current `generation` for optimistic concurrency:

```json
[
  {
    "id": "uuid",
    "rawValue": "Adyen",
    "generation": 3
  }
]
```

If the generation does not match the stored value, the request is rejected with `409 Conflict`.

Users with only the `canChange` role can set values only for entries within services they are associated with. Users with `canAll` can set any entry.

### Update entry metadata

`PUT /api/configentries/{serviceId}/{versionId}/{entryId}`  
**Policy:** `canAll`

Updates display name, description, group, enum definition, and default value. Requires the current `generation`.

### Delete entry

`DELETE /api/configentries/{serviceId}/{versionId}/{entryId}`  
**Policy:** `canAll`

---

## Audit logs

### Search audit logs

`POST /api/auditlogs/search`  
**Policy:** `canChange`

```json
{
  "serviceId": "uuid",
  "entityType": null,
  "pageSize": 50,
  "pageToken": null
}
```

---

## Auth

### Current user

`GET /api/auth/me`

Returns information about the authenticated user including their resolved Konfigo permissions.

---

## Value types

The `valueType` field in config entries uses the following integer codes:

| Code | Type | Notes |
|------|------|-------|
| 0 | Unknown | — |
| 1 | String | Plain text |
| 2 | Boolean | `"true"` / `"false"` |
| 3 | DateTime | ISO 8601 string |
| 4 | TimeSpan | `"HH:mm:ss"` format |
| 5 | Enum | Comma/pipe-separated allowed values stored in `enumDefinition` |
| 6 | Number | Integer or float as string |
| 7 | Array | JSON array string |
| 8 | JSON | Arbitrary JSON object string |

## Error responses

All errors follow the standard ASP.NET Core problem-details format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Generation mismatch"
}
```

Common HTTP status codes:

| Code | Meaning |
|------|---------|
| 400 | Validation error or generation mismatch |
| 401 | Not authenticated |
| 403 | Insufficient permissions |
| 404 | Resource not found |
| 409 | Conflict (e.g. duplicate key) |
