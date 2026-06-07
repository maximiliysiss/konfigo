# Samples

Minimal runnable applications that register a config schema, watch live updates over gRPC,
and expose the bound options at `/options`. Each one points at a local backend started via
`docker compose up -d` from `apps/backend` (see [Deployment](deployment.md)).

| Sample | Description | Source |
|--------|-------------|--------|
| .NET | ASP.NET Core minimal API using `Konfigo.Client` and `IOptionsSnapshot<T>` | [samples/dotnet](https://github.com/maximiliysiss/konfigo/tree/master/samples/dotnet) |
| Go | Go HTTP server using the Go SDK, struct tags, and the gRPC transport | [samples/go](https://github.com/maximiliysiss/konfigo/tree/master/samples/go/README.md) |
| Python | Async Python HTTP server using the Python SDK, dataclasses, and the gRPC transport | [samples/python](https://github.com/maximiliysiss/konfigo/tree/master/samples/python/README.md) |

Each sample README documents how to run it locally, the configuration schema it registers,
and the shape of the `/options` response — follow the links above for the full walkthrough.

## Related SDK guides

- [.NET SDK](sdks/dotnet.md)
- [Go SDK](sdks/go.md)
- [Python SDK](sdks/python.md)
