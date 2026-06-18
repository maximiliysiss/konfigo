using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.Configurations;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Authorization;
using Konfigo.Controllers.Models.Versions;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UpdateVersionRequest = Konfigo.Application.Services.Configurations.Models.UpdateVersionRequest;

namespace Konfigo.Controllers;

[Authorize(Policy = AuthorizationPolicyNames.CanChange)]
[ApiController]
[Route("api/[controller]/{serviceId:guid}")]
public sealed class ConfigVersionsController : ControllerBase
{
    private readonly IConfigVersionService _configVersionService;
    private readonly IConfigVersionsRepository _configVersionsRepository;
    private readonly ILogger<ConfigVersionsController> _logger;

    public ConfigVersionsController(
        IConfigVersionService configVersionService,
        IConfigVersionsRepository configVersionsRepository,
        ILogger<ConfigVersionsController> logger)
    {
        _configVersionService = configVersionService;
        _configVersionsRepository = configVersionsRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ConfigVersion[]> Handle([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var service = new ServiceId(serviceId);
        _logger.LogConfigVersionSearchStarted(service);

        var configVersions = await _configVersionsRepository
            .GetAsync(SearchVersionRequest.Create(serviceId: service), cancellationToken)
            .ToArrayAsync(cancellationToken);

        _logger.LogConfigVersionSearchCompleted(service, configVersions.Length);

        return configVersions;
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanChange)]
    [HttpGet("{versionId:guid}")]
    public async Task<ConfigVersion?> GetById(
        [FromRoute] Guid serviceId,
        [FromRoute] Guid versionId,
        CancellationToken cancellationToken)
    {
        var service = new ServiceId(serviceId);
        var version = new VersionId(versionId);

        _logger.LogConfigVersionGetByIdStarted(service, version);

        var searchVersionRequest = SearchVersionRequest.Create(
            serviceId: service,
            ids: [version]);

        var configVersion = await _configVersionsRepository
            .GetAsync(searchVersionRequest, cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        if (configVersion is null)
            _logger.LogConfigVersionNotFound(service, version);
        else
            _logger.LogConfigVersionGetByIdCompleted(service, configVersion.Id, configVersion.VersionLabel);

        return configVersion;
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanAll)]
    [HttpPost]
    public async Task<ConfigVersion> Create(
        [FromRoute] Guid serviceId,
        [FromBody] CreateConfigVersionRequest request,
        CancellationToken cancellationToken)
    {
        var service = new ServiceId(serviceId);

        var createVersionRequest = new CreateVersionRequest(
            ServiceId: service,
            VersionLabel: request.VersionLabel,
            Description: request.Description,
            CreatedBy: HttpContext.GetUser());

        var result = await _configVersionService.CreateAsync(createVersionRequest, cancellationToken);

        return result;
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanAll)]
    [HttpPut("{versionId:guid}")]
    public async Task<ConfigVersion?> Update(
        [FromRoute] Guid serviceId,
        [FromRoute] Guid versionId,
        [FromBody] UpdateConfigVersionRequest request,
        CancellationToken cancellationToken)
    {
        var service = new ServiceId(serviceId);
        var version = new VersionId(versionId);

        var updateVersionRequest = new UpdateVersionRequest(
            ServiceId: service,
            VersionId: version,
            VersionLabel: request.VersionLabel,
            Description: request.Description,
            UpdatedBy: HttpContext.GetUser());

        var result = await _configVersionService.UpdateAsync(updateVersionRequest, cancellationToken);

        return result;
    }
}
