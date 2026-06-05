namespace Konfigo.IntegrationTests.Shared.Responses;

public sealed record MeResponse(string Id, string? Email, string? Name, string[] Roles, string[] Permissions);
