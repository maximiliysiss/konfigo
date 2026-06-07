# SDKs

Konfigo ships official client SDKs for three languages. Each SDK discovers configuration
schemas from your code, registers them with the backend, loads the current snapshot, and
keeps values up to date over a long-lived gRPC stream.

| SDK | Package | Discovery mechanism |
|-----|---------|---------------------|
| [.NET](dotnet.md) | `Konfigo.Client` / `Konfigo.Abstraction` (NuGet) | `[ConfigGroup]` / `[ConfigKey]` attributes |
| [Go](go.md) | `github.com/maximiliysiss/konfigo/packages/go` | `konfigo` struct tags |
| [Python](python.md) | `konfigo` (PyPI) | `@config_group` / `config_key` decorators |

See the [Samples](../samples.md) page for minimal runnable applications using each SDK.
