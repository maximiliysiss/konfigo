namespace Konfigo.Application.Services.LocalUsers.Models;

public sealed record CreateLocalUserRequest(string Username, string Password, string Role);
