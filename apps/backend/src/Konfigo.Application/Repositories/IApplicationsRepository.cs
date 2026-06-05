using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;

namespace Konfigo.Application.Repositories;

public interface IApplicationsRepository
{
    IAsyncEnumerable<ApplicationService> GetAsync(SearchServiceRequest request, CancellationToken cancellationToken);
    Task<ApplicationService> AddAsync(ApplicationService service, CancellationToken cancellationToken);
    Task UpdateAsync(ApplicationService service, CancellationToken cancellationToken);
    Task DeleteAsync(ApplicationService service, CancellationToken cancellationToken);
}
