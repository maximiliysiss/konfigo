using System;

namespace Konfigo.IntegrationTests.Shared.Responses;

public sealed record AuditEntryResponse(Guid Id, Guid ServiceId);
