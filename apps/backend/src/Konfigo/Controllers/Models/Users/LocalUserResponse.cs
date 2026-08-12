using System;

namespace Konfigo.Controllers.Models.Users;

public sealed class LocalUserResponse
{
    public required string Username { get; set; }
    public required string Role { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
