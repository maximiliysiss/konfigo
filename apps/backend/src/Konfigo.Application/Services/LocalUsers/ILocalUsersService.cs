using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.LocalUsers.Models;
using Konfigo.Domain.Entities;

namespace Konfigo.Application.Services.LocalUsers;

public interface ILocalUsersService
{
    Task<LocalUser[]> GetAllAsync(CancellationToken cancellationToken);
    Task<CreateLocalUserResult> CreateAsync(CreateLocalUserRequest request, CancellationToken cancellationToken);
    Task<LocalUser?> UpdateAsync(UpdateLocalUserRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(DeleteLocalUserRequest request, CancellationToken cancellationToken);
    Task<LocalUser?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken);
    Task EnsureSeededAsync(string username, string password, CancellationToken cancellationToken);
}
