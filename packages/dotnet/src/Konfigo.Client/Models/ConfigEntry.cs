using System;
using ValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.Client.Models;

internal sealed record ConfigEntry(string Key, string? Value, ValueType Type, int Generation, DateTimeOffset Timestamp);
