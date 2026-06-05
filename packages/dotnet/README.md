# Konfigo .NET SDK

.NET packages for consuming Konfigo Realtime Config from applications.

## Packages

| Package | Purpose |
|---------|---------|
| [`Konfigo.Abstraction`](src/Konfigo.Abstraction/README.md) | Attributes for declaring configuration groups and keys. |
| [`Konfigo.Client`](src/Konfigo.Client/README.md) | Runtime SDK that loads configuration over gRPC and applies live updates through `IConfiguration` / `IOptions<T>`. |

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddRealtimeConfig();
builder.Services.AddRealtimeConfig();
```

Configuration classes are declared with attributes from `Konfigo.Abstraction`:

```csharp
using Konfigo.Abstraction.Attributes;

[ConfigGroup(key: "Payments", groupName: "Payments", description: "Payment gateway")]
public sealed class PaymentsOptions
{
    [ConfigKey(description: "Payment provider", defaultValue: "Stripe")]
    public string Provider { get; set; } = string.Empty;
}
```
