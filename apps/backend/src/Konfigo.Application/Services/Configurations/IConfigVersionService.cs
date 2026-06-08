using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Domain.Entities;

namespace Konfigo.Application.Services.Configurations;

public interface IConfigVersionService
{
    Task<ConfigVersion> CreateAsync(CreateVersionRequest request, CancellationToken cancellationToken);
    Task<ConfigVersion?> UpdateAsync(UpdateVersionRequest request, CancellationToken cancellationToken);
    Task<GenerateResult> GenerateAsync(GenerateVersionRequest request, CancellationToken cancellationToken);
}
