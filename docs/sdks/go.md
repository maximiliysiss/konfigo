# Go SDK

The Go SDK discovers configuration schemas from `konfigo` struct tags, registers them with
the backend, watches for live updates over the gRPC transport, and binds snapshots back into
typed structs.

!!! info "Full guide"
    The complete SDK guide — installation, transports, discovery, and binding details — lives
    in the package source:
    [packages/go/README.md](https://github.com/maximiliysiss/konfigo/blob/master/packages/go/README.md)

## Quick start

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

## See also

- [packages/go](https://github.com/maximiliysiss/konfigo/tree/master/packages/go) — SDK source
- [samples/go](https://github.com/maximiliysiss/konfigo/tree/master/samples/go) — runnable HTTP server sample
- [gRPC Protocol — Go connection](../grpc.md#go)
