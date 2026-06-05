# Konfigo.Client

Client SDK for the Konfigo Realtime Config service. It plugs configuration classes annotated with the attributes from [`Konfigo.Abstraction`](../Konfigo.Abstraction/README.md) into the standard `IConfiguration` / `IOptions<T>` pipeline and applies updates at runtime without restarting the host.

## How it works

1. At host startup `AddRealtimeConfig()` on `IConfigurationBuilder` reads the `RealtimeConfigOptions` section and, if the SDK is enabled, registers a gRPC-backed configuration source. The source pulls the initial snapshot and exposes it through `IConfiguration`.
2. `AddRealtimeConfig()` on `IServiceCollection` registers a hosted service that:
   - Uses reflection to find classes annotated with `[ConfigGroup]`, describes their schema, and registers a version on the backend (or reuses an existing one if the same version is already known — concurrent startups are guarded by a distributed lock).
   - Subscribes to the update event stream and applies events to the provider. Each key carries a `Generation`; stale events are ignored to avoid races on reconnect.
   - Binds each group to `IOptions<T>` under the key from `ConfigGroupAttribute.Key`.

`IOptionsMonitor<T>` propagates updates correctly — `OnChange` handlers fire after the provider calls `OnReload()`.

## Setup

```csharp
// Program.cs

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddRealtimeConfig();
builder.Services.AddRealtimeConfig();

var app = builder.Build();
app.Run();
```

Minimal `appsettings.json`:

```json
{
  "RealtimeConfigOptions": {
    "IsEnabled": true,
    "ServiceId": "orders",
    "Version": "1.0.0",
    "Url": "https://realtime-config.internal",
    "PollingInterval": "00:00:05"
  }
}
```

| Field             | Description                                                                |
|-------------------|----------------------------------------------------------------------------|
| `IsEnabled`       | Master switch. When `false`, the SDK does not connect.                     |
| `ServiceId`       | Service identifier in Realtime Config (used for routing).                  |
| `Version`         | Schema version. Bumped on breaking changes to the set of keys.             |
| `Url`             | gRPC endpoint of the Realtime Config service.                              |
| `Timestamp`       | Starting timestamp for the subscription. Usually left at the default.      |
| `PollingInterval` | Delay between subscription retries after an error. Defaults to 5 seconds.  |

### Tuning the version registration lock

Version registration is guarded by a distributed lock so that concurrent instances of the same service do not register the same version twice. The lock-acquisition timeout defaults to 30 seconds and can be overridden via the `VersionServiceOptions` section:

```json
{
  "VersionServiceOptions": {
    "LockTimeout": "00:01:00"
  }
}
```

## Declaring configuration classes

Classes and properties are declared with the attributes from `Konfigo.Abstraction`:

```csharp
using Konfigo.Abstraction.Attributes;

[ConfigGroup(key: "Payments", groupName: "Payments", description: "Payment gateway")]
public sealed class PaymentsOptions
{
    [ConfigKey(description: "Payment provider", defaultValue: "Stripe")]
    public string Provider { get; set; } = string.Empty;

    [ConfigKey(description: "Request timeout", defaultValue: "00:00:30")]
    public TimeSpan Timeout { get; set; }
}
```

The class is bound automatically — no explicit `IOptions<PaymentsOptions>` registration is required. Consumption is standard:

```csharp
public sealed class CheckoutService(IOptionsMonitor<PaymentsOptions> options)
{
    public Task ChargeAsync()
    {
        var snapshot = options.CurrentValue;
        // ...
    }
}
```

## Public API

| Type / method                                              | Purpose                                                                    |
|------------------------------------------------------------|----------------------------------------------------------------------------|
| `ConfigurationBuilderExtensions.AddRealtimeConfig()`       | Adds the gRPC-backed configuration source to `IConfigurationBuilder`.      |
| `ServiceCollectionExtensions.AddRealtimeConfig()`          | Registers the hosted service, options bindings, and infrastructure services. |
| `ServiceCollectionExtensions.AddRtcOptions()`              | Binds discovered `[ConfigGroup]` classes to their `IOptions<T>` sections.  |

## Failure modes

- When `IsEnabled = false` the hosted service parks on `Task.Delay(Infinite)` and stays idle.
- If no class annotated with `[ConfigGroup]` is found, the service logs a warning and does not start. This is a safe no-op rather than a host crash.
- When the subscription stream drops, the service waits for `PollingInterval` and re-establishes the subscription. The local `timestamp` is preserved so it resumes from where it left off after a reconnect.
- Events with `Generation == 1` are treated as the initial state and do not overwrite data already loaded via the initial snapshot.

## Related packages

- [`Konfigo.Abstraction`](../Konfigo.Abstraction/README.md) — the `ConfigGroup` / `ConfigKey` attributes.
