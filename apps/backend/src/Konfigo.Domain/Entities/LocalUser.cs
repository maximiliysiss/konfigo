using System;

namespace Konfigo.Domain.Entities;

public sealed class LocalUser
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public void SetPassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        UpdatedAt = now;
    }

    public void SetRole(string role, DateTimeOffset now)
    {
        Role = role;
        UpdatedAt = now;
    }
}
