using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Client.Entities;
using Konfigo.Client.Extensions;
using Konfigo.Client.Grpc;
using Konfigo.Client.Infrastructure.Assemblies;
using Konfigo.Client.Infrastructure.Client;
using Konfigo.Client.Models;
using Konfigo.Client.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Konfigo.Client.Infrastructure.Versions;

internal sealed class VersionService : IVersionService
{
    private readonly RealtimeConfigOptions _rtcOptions;

    private readonly IRealtimeConfigClient _service;

    private readonly ILogger<VersionService> _logger;

    private readonly IAssemblyService _assemblyService;

    public VersionService(
        IOptions<RealtimeConfigOptions> rtcOptions,
        IRealtimeConfigClient service,
        ILogger<VersionService> logger,
        IAssemblyService assemblyService)
    {
        _service = service;
        _logger = logger;
        _assemblyService = assemblyService;
        _rtcOptions = rtcOptions.Value;
    }

    public async Task<VersionId> CreateAsync(CancellationToken cancellationToken)
    {
        var classDefinitions = _assemblyService.GetDefinitions();

        var isVersionExistRequest = new IsVersionExistRequest
        {
            ServiceId = _rtcOptions.ServiceId,
            Version = _rtcOptions.Version,
        };

        var isVersionExistResponse = await _service.IsVersionExistsAsync(
            request: isVersionExistRequest,
            cancellationToken: cancellationToken);

        if (isVersionExistResponse.VersionId is not null)
        {
            _logger.RealtimeConfigAlreadyExists();
            return new VersionId(isVersionExistResponse.VersionId);
        }

        _logger.RealtimeConfigDoesNotExistCreating();

        var createVersionRequest = new CreateVersionRequest
        {
            ServiceId = _rtcOptions.ServiceId,
            Version = _rtcOptions.Version,
            Classes = { classDefinitions.Select(MapClass) },
        };

        var version = await _service.CreateVersionAsync(
            request: createVersionRequest,
            cancellationToken: cancellationToken);

        _logger.RealtimeConfigCreated();

        return new VersionId(version.VersionId);

        static CreateVersionRequest.Types.ClassEntry MapClass(ClassDefinition definition)
        {
            return new CreateVersionRequest.Types.ClassEntry
            {
                Description = definition.Description,
                Name = definition.Name,
                Entries = { definition.Options.Select(MapConfig) },
            };
        }

        static CreateVersionRequest.Types.ClassEntry.Types.ConfigEntry MapConfig(ClassDefinition.OptionDefinition option)
        {
            return new CreateVersionRequest.Types.ClassEntry.Types.ConfigEntry
            {
                Description = option.Description,
                Key = option.Key,
                Name = option.Name,
                ValueType = option.Type,
                EnumValues = string.Join(",", option.EnumValues ?? []),
                Value = option.DefaultValue,
            };
        }
    }
}
