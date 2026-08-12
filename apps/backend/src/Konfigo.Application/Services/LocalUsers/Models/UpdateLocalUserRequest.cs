namespace Konfigo.Application.Services.LocalUsers.Models;

public sealed record UpdateLocalUserRequest(string Username, string? Password, string? Role);
