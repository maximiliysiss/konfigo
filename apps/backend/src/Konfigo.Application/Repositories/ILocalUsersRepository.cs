using System.Threading;
using System.Threading.Tasks;
using Konfigo.Domain.Entities;

namespace Konfigo.Application.Repositories;

public interface ILocalUsersRepository
{
    Task<LocalUser[]> GetAllAsync(CancellationToken cancellationToken);
    Task<LocalUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task AddAsync(LocalUser user, CancellationToken cancellationToken);
    Task UpdateAsync(LocalUser user, CancellationToken cancellationToken);
    Task DeleteAsync(LocalUser user, CancellationToken cancellationToken);
}
