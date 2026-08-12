namespace Konfigo.Controllers.Models.Users;

public sealed class UpdateLocalUserRequest
{
    public string? Password { get; set; }
    public string? Role { get; set; }
}
