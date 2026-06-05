using System;

namespace Konfigo.IntegrationTests.Shared.Responses;

public sealed record VersionResponse(Guid Id, string VersionLabel);
