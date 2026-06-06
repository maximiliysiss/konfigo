# Konfigo Go SDK

Go client SDK for Konfigo Realtime Config. It mirrors the .NET SDK concepts:

- config groups and keys are declared with struct tags;
- schemas are discovered and registered as a backend version;
- config values are stored as string entries with per-key generations;
- stale and equal-generation updates are ignored;
- subscription events with generation `1` are treated as initial state and do not overwrite the already loaded snapshot.

## Package metadata

Module path: `github.com/maximiliysiss/konfigo/packages/go`

Go package versions are published through git tags. Use the repository release version from
`../../VERSION` for module tags, for example `v0.0.1`.

## Declaring Options

Go does not have attributes, so a config group is marked with an embedded `konfigo.Group` field. Exported fields with a `konfigo` tag become config keys.

```go
package main

import (
	"time"

	konfigo "github.com/maximiliysiss/konfigo/packages/go"
)

type PaymentsOptions struct {
	konfigo.Group `konfigo:"key=Payments,name=Payments,description=Payment gateway"`

	Provider string        `konfigo:"key,name=Provider,description=Payment provider,default=Stripe"`
	Timeout  time.Duration `konfigo:"key,name=Timeout,default=00:00:30"`
}
```

Supported tag fields:

| Field | Purpose |
|-------|---------|
| `key` | Marks a struct field as a config entry. On `Group`, overrides the section key. |
| `name` | Display name for a group or key. |
| `description` | Human-readable description. |
| `default` | String-encoded default value. |
| `type` | Optional override: `string`, `boolean`, `date_time`, `time_span`, `enum`, `number`, `array`, `json`. |
| `enum` | Enum display values separated by `|`, e.g. `enum=Stripe|Adyen`. |

## Runtime Usage

```go
ctx := context.Background()

definitions, err := konfigo.DiscoverDefinitions(PaymentsOptions{})
if err != nil {
	return err
}

conn, err := grpc.NewClient("realtime-config.internal:443")
if err != nil {
	return err
}
defer conn.Close()

transport := konfigo.NewGrpcTransport(conn)
client, err := konfigo.NewClientFromRemote(ctx, konfigo.RealtimeConfigOptions{
	IsEnabled: true,
	ServiceID: "orders",
	Version:   "1.0.0",
}, transport, definitions)
if err != nil {
	return err
}

go func() {
	_ = client.Watch(ctx)
}()

var payments PaymentsOptions
if err := konfigo.BindConfig(client.Store.Snapshot(), &payments); err != nil {
	return err
}
```

Applications can subscribe to store changes and rebind options:

```go
unsubscribe := client.Store.Subscribe(func(snapshot map[string]*string) {
	var payments PaymentsOptions
	_ = konfigo.BindConfig(snapshot, &payments)
})
defer unsubscribe()
```

## gRPC

`GrpcTransport` is generated from `protos/service.proto`, which is wire-compatible with the .NET SDK proto. The transport exposes the same operations used by the core client:

- `GetConfig` for the initial snapshot;
- `IsVersionExists` and `CreateVersion` for schema version registration;
- `StartSubscribe` for live updates.
