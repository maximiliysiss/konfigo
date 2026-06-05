# gRPC Protocol

All three client SDKs communicate with the backend over gRPC using a shared protobuf definition. The proto file lives at:

- `packages/go/protos/service.proto`
- `packages/dotnet/src/Konfigo.Client/protos/service.proto`
- `packages/python/protos/service.proto`

All three files are identical — they define the same wire protocol.

## Service definition

```protobuf
service RealtimeConfigGrpcService {
  rpc GetConfig(GetConfigRequest) returns (GetConfigResponse);
  rpc IsVersionExists(IsVersionExistRequest) returns (IsVersionExistResponse);
  rpc CreateVersion(CreateVersionRequest) returns (CreateVersionResponse);
  rpc StartSubscribe(StartSubscribeRequest) returns (stream SubscriptionEvent);
}
```

## RPCs

### GetConfig

Fetches the current snapshot for a service version.

**Request:**

```protobuf
message GetConfigRequest {
  string service_id = 1;  // UUID string
  string version    = 2;  // Version label, e.g. "1.0.0"
}
```

**Response:**

```protobuf
message GetConfigResponse {
  repeated ConfigEntry entries = 1;

  message ConfigEntry {
    string                    key        = 1;
    google.protobuf.StringValue value     = 2;  // nullable
    int32                     generation = 3;
    google.protobuf.Timestamp timestamp  = 4;
  }
}
```

Returns an empty `entries` list if the version does not exist yet.

---

### IsVersionExists

Checks whether a version with the given label is already registered.

**Request:**

```protobuf
message IsVersionExistRequest {
  string service_id = 1;
  string version    = 2;
}
```

**Response:**

```protobuf
message IsVersionExistResponse {
  google.protobuf.StringValue version_id = 1;  // null when not found
}
```

If `version_id` is non-null the SDK reuses it and skips `CreateVersion`.

---

### CreateVersion

Registers a new config schema version derived from the annotated classes in the application.

**Request:**

```protobuf
message CreateVersionRequest {
  string          service_id = 1;
  string          version    = 2;
  repeated ClassEntry classes = 3;

  message ClassEntry {
    string                      name        = 1;
    google.protobuf.StringValue description = 2;
    repeated ConfigEntry        entries     = 3;

    message ConfigEntry {
      string                      key         = 1;
      string                      name        = 2;
      google.protobuf.StringValue description = 3;
      ValueType                   value_type  = 4;
      google.protobuf.StringValue enum_values = 5;  // pipe-separated e.g. "Stripe|Adyen"
      google.protobuf.StringValue value       = 6;  // default value
    }
  }
}
```

**Response:**

```protobuf
message CreateVersionResponse {
  string version_id = 1;  // UUID of the created version
}
```

The backend auto-generates the version label and description. Existing entries (same key) are preserved; new keys are added. Calling `CreateVersion` for an existing version label returns the existing version's ID.

---

### StartSubscribe

Opens a server-streaming RPC that delivers config change events in real time.

**Request:**

```protobuf
message StartSubscribeRequest {
  string                    service_id = 1;
  string                    version_id = 2;  // UUID from IsVersionExists or CreateVersion
  google.protobuf.Timestamp timestamp  = 3;  // only send events after this time
}
```

**Stream events:**

```protobuf
message SubscriptionEvent {
  repeated ConfigEvent events = 1;

  message ConfigEvent {
    string                    key        = 1;
    google.protobuf.StringValue value    = 2;  // null means the key was deleted
    int32                     generation = 3;
    google.protobuf.Timestamp timestamp  = 4;
  }
}
```

**Backfill:** On connection, the server immediately sends all entries updated after `timestamp` as a single initial `SubscriptionEvent`. The SDK ignores these for generation tracking because they are treated as catch-up, not new changes — entries with `generation == 1` from the initial `GetConfig` snapshot are preferred.

**Reconnect:** If the stream drops (network error, server restart), the SDK sleeps for `PollingInterval` (default 5 s) and reconnects, passing the last known `timestamp` so no changes are missed.

## ValueType enum

```protobuf
enum ValueType {
  VALUE_TYPE_UNKNOWN   = 0;
  VALUE_TYPE_STRING    = 1;
  VALUE_TYPE_BOOLEAN   = 2;
  VALUE_TYPE_DATE_TIME = 3;
  VALUE_TYPE_TIME_SPAN = 4;
  VALUE_TYPE_ENUM      = 5;
  VALUE_TYPE_NUMBER    = 6;
  VALUE_TYPE_ARRAY     = 7;
  VALUE_TYPE_JSON      = 8;
}
```

## Connection

The backend exposes HTTP/2 (gRPC) on the same port as HTTP/1.1 REST (`:8080`) using ASP.NET Core's protocol negotiation. Standard gRPC client libraries connect without TLS termination issues when targeting `http://` in development. In production use TLS and an appropriate gRPC channel configuration.

### Go

```go
conn, err := grpc.NewClient("realtime-config.internal:443",
    grpc.WithTransportCredentials(credentials.NewClientTLSFromCert(nil, "")))
```

### .NET

Configured automatically via `AddRealtimeConfig()`. The URL comes from `RealtimeConfig:Url` in `appsettings.json`.

### Python

```python
transport = GrpcRealtimeConfigTransport("realtime-config.internal:443")
```

Pass a plaintext address for local development; for TLS add the appropriate gRPC channel credentials to `GrpcRealtimeConfigTransport`.
