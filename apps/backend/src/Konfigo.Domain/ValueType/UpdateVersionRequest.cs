namespace Konfigo.Domain.ValueType;

public sealed record UpdateVersionRequest(
    string VersionLabel,
    string? Description);
