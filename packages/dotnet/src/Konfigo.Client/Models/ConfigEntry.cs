using System;

namespace Konfigo.Client.Models;

internal sealed record ConfigEntry(string Key, string? Value, int Generation, DateTimeOffset Timestamp);
