using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Domain.Entities;

namespace Konfigo.Application.Services.Configurations;

public interface IConfigEntryService
{
    Task<ConfigEntry> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken);
    Task<ConfigEntry?> UpdateAsync(UpdateEntryRequest request, CancellationToken cancellationToken);
    Task<ConfigEntry[]> SetAsync(SetEntryRequest request, CancellationToken cancellationToken);
    Task<ConfigEntry?> DeleteAsync(DeleteEntryRequest request, CancellationToken cancellationToken);
}
