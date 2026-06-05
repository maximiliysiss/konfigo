using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.ApplicationServices.Models;
using Konfigo.Domain.Entities;
using UpdateServiceRequest = Konfigo.Application.Services.ApplicationServices.Models.UpdateServiceRequest;

namespace Konfigo.Application.Services.ApplicationServices;

public interface IApplicationsService
{
    Task<ApplicationService> AddAsync(CreateServiceRequest request, CancellationToken cancellationToken);
    Task<ApplicationService?> UpdateAsync(UpdateServiceRequest request, CancellationToken cancellationToken);
    Task<ApplicationService?> DeleteAsync(DeleteServiceRequest request, CancellationToken cancellationToken);
}
