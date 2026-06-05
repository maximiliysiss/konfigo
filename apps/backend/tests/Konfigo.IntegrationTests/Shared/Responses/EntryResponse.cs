using System;

namespace Konfigo.IntegrationTests.Shared.Responses;

public sealed record EntryResponse(Guid Id, string Key, string Name, int Generation);
