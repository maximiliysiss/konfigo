using Konfigo.Domain.Entities;

namespace Konfigo.Application.Services.Configurations.Models;

public abstract record GenerateResult(ConfigVersion Version)
{
    public sealed record New(ConfigVersion Version) : GenerateResult(Version);

    public sealed record Exists(ConfigVersion Version) : GenerateResult(Version);
}
