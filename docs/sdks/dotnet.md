# .NET SDK

`Konfigo.Client` (with `Konfigo.Abstraction`) is the official .NET client for Konfigo,
distributed via NuGet. It scans your options classes for `[ConfigGroup]` / `[ConfigKey]`
attributes, registers the schema with the backend, and keeps `IOptions<T>` /
`IOptionsSnapshot<T>` instances live over a gRPC stream.

!!! info "Full guide"
    The complete SDK guide — installation, configuration, transports, and binding details —
    lives in the package source:
    [packages/dotnet/README.md](https://github.com/maximiliysiss/konfigo/blob/master/packages/dotnet/README.md)

## Quick start

```csharp
// Program.cs
builder.Configuration.AddRealtimeConfig();
builder.Services.AddRealtimeConfig();

// Options class
[ConfigGroup(key: "Payments", groupName: "Payments")]
public sealed class PaymentsOptions
{
    [ConfigKey(description: "Payment provider", defaultValue: "Stripe")]
    public string Provider { get; set; } = string.Empty;
}

// Usage
var payments = app.Services.GetRequiredService<IOptions<PaymentsOptions>>().Value;
```

## See also

- [packages/dotnet/src/Konfigo.Client](https://github.com/maximiliysiss/konfigo/tree/master/packages/dotnet/src/Konfigo.Client)
- [packages/dotnet/src/Konfigo.Abstraction](https://github.com/maximiliysiss/konfigo/tree/master/packages/dotnet/src/Konfigo.Abstraction)
- [samples/dotnet](https://github.com/maximiliysiss/konfigo/tree/master/samples/dotnet) — runnable ASP.NET Core sample
