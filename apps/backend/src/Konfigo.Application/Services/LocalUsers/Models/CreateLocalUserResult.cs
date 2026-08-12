using Konfigo.Domain.Entities;

namespace Konfigo.Application.Services.LocalUsers.Models;

public abstract record CreateLocalUserResult
{
    public sealed record Created(LocalUser User) : CreateLocalUserResult;

    public sealed record AlreadyExists : CreateLocalUserResult;

    public sealed record InvalidRole : CreateLocalUserResult;
}
