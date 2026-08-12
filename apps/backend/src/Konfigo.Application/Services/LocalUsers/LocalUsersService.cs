using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Extensions;
using Konfigo.Application.Infrastructure.DateTime;
using Konfigo.Application.Infrastructure.Security;
using Konfigo.Application.Repositories;
using Konfigo.Application.Services.LocalUsers.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Microsoft.Extensions.Logging;

namespace Konfigo.Application.Services.LocalUsers;

internal sealed class LocalUsersService : ILocalUsersService
{
    private readonly ILocalUsersRepository _repository;

    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly ILogger<LocalUsersService> _logger;

    public LocalUsersService(
        ILocalUsersRepository repository,
        IDateTimeProvider dateTimeProvider,
        ILogger<LocalUsersService> logger)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public Task<LocalUser[]> GetAllAsync(CancellationToken cancellationToken) => _repository.GetAllAsync(cancellationToken);

    public async Task<CreateLocalUserResult> CreateAsync(CreateLocalUserRequest request, CancellationToken cancellationToken)
    {
        _logger.LogLocalUserCreateStarted(request.Username);

        if (!LocalUserRoles.IsValid(request.Role))
        {
            _logger.LogLocalUserInvalidRole(request.Username, request.Role);
            return new CreateLocalUserResult.InvalidRole();
        }

        var existing = await _repository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existing is not null)
        {
            _logger.LogLocalUserAlreadyExists(request.Username);
            return new CreateLocalUserResult.AlreadyExists();
        }

        var now = _dateTimeProvider.GetNow();

        var user = new LocalUser
        {
            Username = request.Username,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role,
            CreatedAt = now,
        };

        await _repository.AddAsync(user, cancellationToken);

        _logger.LogLocalUserCreated(user.Username, user.Role);

        return new CreateLocalUserResult.Created(user);
    }

    public async Task<LocalUser?> UpdateAsync(UpdateLocalUserRequest request, CancellationToken cancellationToken)
    {
        _logger.LogLocalUserUpdateStarted(request.Username);

        if (request.Role is not null && !LocalUserRoles.IsValid(request.Role))
        {
            _logger.LogLocalUserInvalidRole(request.Username, request.Role);
            return null;
        }

        var user = await _repository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null)
        {
            _logger.LogLocalUserNotFound(request.Username);
            return null;
        }

        var now = _dateTimeProvider.GetNow();

        if (request.Role is not null)
            user.SetRole(request.Role, now);

        if (!string.IsNullOrEmpty(request.Password))
            user.SetPassword(PasswordHasher.Hash(request.Password), now);

        await _repository.UpdateAsync(user, cancellationToken);

        _logger.LogLocalUserUpdated(user.Username);

        return user;
    }

    public async Task<bool> DeleteAsync(DeleteLocalUserRequest request, CancellationToken cancellationToken)
    {
        _logger.LogLocalUserDeleteStarted(request.Username);

        var user = await _repository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null)
        {
            _logger.LogLocalUserNotFound(request.Username);
            return false;
        }

        await _repository.DeleteAsync(user, cancellationToken);

        _logger.LogLocalUserDeleted(user.Username);

        return true;
    }

    public async Task<LocalUser?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByUsernameAsync(username, cancellationToken);
        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            _logger.LogLocalUserLoginFailed(username);
            return null;
        }

        _logger.LogLocalUserLoginSucceeded(username);

        return user;
    }

    public async Task EnsureSeededAsync(string username, string password, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByUsernameAsync(username, cancellationToken);
        if (existing is not null)
            return;

        var user = new LocalUser
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            Role = LocalUserRoles.Admin,
            CreatedAt = _dateTimeProvider.GetNow(),
        };

        await _repository.AddAsync(user, cancellationToken);

        _logger.LogLocalUserSeeded(username);
    }
}
