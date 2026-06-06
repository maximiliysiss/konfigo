# Konfigo .NET SDK

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](src/Konfigo.Client/Konfigo.Client.csproj)
[![Konfigo.Client](https://img.shields.io/badge/NuGet-Konfigo.Client-004880?logo=nuget)](https://www.nuget.org/packages/Konfigo.Client)
[![Konfigo.Abstraction](https://img.shields.io/badge/NuGet-Konfigo.Abstraction-004880?logo=nuget)](https://www.nuget.org/packages/Konfigo.Abstraction)
[![CI/CD](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml/badge.svg)](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-0.0.1-blue)](../../VERSION)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](../../LICENSE)

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
