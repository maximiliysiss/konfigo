# Konfigo.Abstraction

[![NuGet](https://img.shields.io/badge/NuGet-Konfigo.Abstraction-004880?logo=nuget)](https://www.nuget.org/packages/Konfigo.Abstraction)
[![Target Framework](https://img.shields.io/badge/target-netstandard2.0-512BD4?logo=dotnet)](Konfigo.Abstraction.csproj)
[![CI/CD](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml/badge.svg)](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-0.0.1-blue)](../../../../VERSION)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](../../../../LICENSE)

A minimal assembly with the attributes used to mark classes and properties that declare configuration for the Konfigo Realtime Config service. It contains only schema attributes, so it can be safely referenced from projects that declare configuration contracts without pulling in the runtime SDK (`Konfigo.Client`).

## Why it exists

The Realtime Config service stores settings centrally and streams updates to subscribers. To bind those settings to a .NET class and publish their schema in the Realtime Config UI, the class is marked with `[ConfigGroup]` and its properties with `[ConfigKey]`. At startup the SDK discovers such classes via reflection, registers their version on the backend, and wires them into `IConfiguration` / `IOptions<T>`.

## Public API

| Attribute              | Purpose                                                       |
|------------------------|---------------------------------------------------------------|
| `ConfigGroupAttribute` | Marks a class as a group of settings (an `IConfiguration` section). |
| `ConfigKeyAttribute`   | Marks a property as an individual key inside a group.         |

### `ConfigGroupAttribute`

```csharp
[ConfigGroup(key: "Payments", groupName: "Payments", description: "Payment gateway")]
public sealed class PaymentsOptions
{
    // ...
}
```

- `key` — `IConfiguration` section name the class binds to. Falls back to the class name when omitted.
- `groupName` — display name shown in the UI.
- `description` — description of the group shown in the UI.

### `ConfigKeyAttribute`

```csharp
[ConfigGroup("Payments")]
public sealed class PaymentsOptions
{
    [ConfigKey(name: "Provider", description: "Payment provider", defaultValue: "Stripe")]
    public string Provider { get; set; } = string.Empty;

    [ConfigKey(description: "Request timeout in seconds", defaultValue: "30")]
    public int TimeoutSeconds { get; set; }
}
```

- `name` — display name shown in the UI; defaults to the property name.
- `description` — description of the key.
- `defaultValue` — string-encoded default value. When omitted, the SDK substitutes a type-driven default (`"0"` for numbers, `"false"` for `bool`, `"[]"` for arrays, and so on).

## Supported property types

The property type determines how a value is rendered in the UI and how it is validated:

- `string` → `String`
- `bool` → `Boolean`
- numbers (`int`, `long`, `double`, `decimal`, …) → `Number`
- `DateTime`, `DateTimeOffset` → `DateTime`
- `TimeSpan` → `TimeSpan`
- `enum` → `Enum` (values are exposed as the list of names)
- arrays → `Array`
- anything else → `Json`

`Nullable<T>` is supported: when no default is provided, the value stays `null`.

## Related packages

- [`Konfigo.Client`](../Konfigo.Client/README.md) — the runtime SDK: gRPC client, configuration source, and the hosted service that applies updates.
