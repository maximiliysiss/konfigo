using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.Configurations;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Authorization;
using Konfigo.Controllers.Models.Entry;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UpdateEntryRequest = Konfigo.Application.Services.Configurations.Models.UpdateEntryRequest;

namespace Konfigo.Controllers;

[Authorize(Policy = AuthorizationPolicyNames.CanChange)]
[ApiController]
[Route("api/[controller]/{serviceId:guid}/{versionId:guid}")]
public sealed class ConfigEntriesController : ControllerBase
{
    private readonly IConfigEntryService _configEntryService;

    private readonly IConfigEntryRepository _configEntryRepository;

    private readonly ILogger<ConfigEntriesController> _logger;

    private readonly KonfigoAuthenticationOptions _options;

    public ConfigEntriesController(
        IConfigEntryService configEntryService,
        IConfigEntryRepository configEntryRepository,
        ILogger<ConfigEntriesController> logger,
        IOptions<KonfigoAuthenticationOptions> options)
    {
        _configEntryService = configEntryService;
        _configEntryRepository = configEntryRepository;
        _logger = logger;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<ConfigEntry[]> Handle([FromRoute] Guid serviceId, [FromRoute] Guid versionId, CancellationToken cancellationToken)
    {
        var service = new ServiceId(serviceId);
        var version = new VersionId(versionId);

        _logger.LogConfigEntrySearchStarted(service, version);

        var searchEntryRequest = SearchEntryRequest.Create(
            service,
            version,
            asTracking: false);

        var result = await _configEntryRepository
            .GetAsync(searchEntryRequest, cancellationToken)
            .ToArrayAsync(cancellationToken);

        _logger.LogConfigEntrySearchCompleted(service, version, result.Length);

        return result;
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanAll)]
    [HttpPost]
    public async Task<ConfigEntry> Create(
        [FromRoute] Guid serviceId,
        [FromRoute] Guid versionId,
        [FromBody] CreateConfigEntryRequest request,
        CancellationToken cancellationToken)
    {
        var service = new ServiceId(serviceId);
        var version = new VersionId(versionId);

        var createEntryRequest = new CreateEntryRequest(
            ServiceId: service,
            VersionId: version,
            Key: request.Key,
            Name: request.Name,
            RawValue: request.RawValue,
            ValueType: request.ValueType,
            EnumDefinition: request.EnumDefinition,
            Description: request.Description,
            GroupName: request.GroupName,
            GroupDescription: request.GroupDescription,
            CreatedBy: User.GetId());

        var result = await _configEntryService.CreateAsync(createEntryRequest, cancellationToken);

        return result;
    }

    [HttpPut("set")]
    public async Task<ActionResult<ConfigEntry[]>> Set(
        [FromRoute] Guid serviceId,
        [FromRoute] Guid versionId,
        [FromBody] SetConfigEntryRequest[] request,
        CancellationToken cancellationToken)
    {
        var service = new ServiceId(serviceId);
        var version = new VersionId(versionId);

        var setEntryRequest = new SetEntryRequest(
            ServiceId: service,
            VersionId: version,
            Requests: request.Select(Map).ToArray(),
            UpdatedBy: User.GetMemberId(_options));

        try
        {
            var result = await _configEntryService.SetAsync(setEntryRequest, cancellationToken);

            return result;
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        static SetEntryRequest.SetRequest Map(SetConfigEntryRequest c)
        {
            return new SetEntryRequest.SetRequest(
                Id: new EntryId(c.Id),
                RawValue: c.RawValue,
                Generation: c.Generation);
        }
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanAll)]
    [HttpPut("{entryId:guid}")]
    public async Task<ConfigEntry?> Update(
        [FromRoute] Guid serviceId,
        [FromRoute] Guid versionId,
        [FromRoute] Guid entryId,
        [FromBody] UpdateConfigEntryRequest request,
        CancellationToken cancellationToken)
    {
        var service = new ServiceId(serviceId);
        var version = new VersionId(versionId);

        var updateEntryRequest = new UpdateEntryRequest(
            ServiceId: service,
            VersionId: version,
            Id: new EntryId(entryId),
            RawValue: request.RawValue,
            EnumDefinition: request.EnumDefinition,
            Description: request.Description,
            GroupName: request.GroupName,
            GroupDescription: request.GroupDescription,
            Generation: request.Generation,
            UpdatedBy: User.GetId());

        var result = await _configEntryService.UpdateAsync(updateEntryRequest, cancellationToken);

        return result;
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanAll)]
    [HttpDelete("{entryId:guid}")]
    public async Task Delete(
        [FromRoute] Guid serviceId,
        [FromRoute] Guid versionId,
        [FromRoute] Guid entryId,
        CancellationToken cancellationToken)
    {
        var service = new ServiceId(serviceId);
        var version = new VersionId(versionId);

        var deleteEntryRequest = new DeleteEntryRequest(
            ServiceId: service,
            VersionId: version,
            Id: new EntryId(entryId),
            DeletedBy: User.GetId());

        await _configEntryService.DeleteAsync(deleteEntryRequest, cancellationToken);
    }
}
