using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.ApplicationServices;
using Konfigo.Application.Services.ApplicationServices.Models;
using Konfigo.Authorization;
using Konfigo.Controllers.Converters;
using Konfigo.Controllers.Models.Services;
using Konfigo.Controllers.Models.Shared;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UpdateServiceRequest = Konfigo.Application.Services.ApplicationServices.Models.UpdateServiceRequest;

namespace Konfigo.Controllers;

[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicyNames.CanChange)]
[ApiController]
public sealed class ServicesController : ControllerBase
{
    private readonly IApplicationsService _applicationsService;
    private readonly IApplicationsRepository _applicationsRepository;

    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IApplicationsService applicationsService,
        IApplicationsRepository applicationsRepository,
        ILogger<ServicesController> logger)
    {
        _applicationsService = applicationsService;
        _applicationsRepository = applicationsRepository;
        _logger = logger;
    }

    [HttpPost("search")]
    public async Task<PageResponse<ApplicationService>> Handle(SearchServicesRequest contract, CancellationToken cancellationToken)
    {
        var pageToken = contract.PageToken.AsPageToken(SearchServiceRequest.PageToken.Empty);

        var labels = User.GetServices();
        if (labels is [])
            return PageResponse<ApplicationService>.Empty;

        var searchServiceRequest = SearchServiceRequest.Create(
            name: contract.Name,
            pageSize: contract.PageSize,
            labels: labels,
            cursor: pageToken,
            asTracking: false);

        var services = await _applicationsRepository
            .GetAsync(searchServiceRequest, cancellationToken)
            .ToArrayAsync(cancellationToken);

        _logger.LogApplicationServiceSearchCompleted(contract.Name, contract.PageSize, services.Length);

        var nextPageToken = services.Length < contract.PageSize
            ? string.Empty
            : new SearchServiceRequest.PageToken(services[^1].Num).AsBase64();

        return new PageResponse<ApplicationService>
        {
            Entities = services,
            NextPageToken = nextPageToken,
        };
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanChange)]
    [HttpGet("{serviceId:guid}")]
    public async Task<ApplicationService?> GetById([FromRoute] Guid serviceId, CancellationToken cancellationToken)
    {
        var id = new ServiceId(serviceId);
        _logger.LogApplicationServiceGetByIdStarted(id);

        var searchServiceRequest = SearchServiceRequest.Create(
            ids: [id],
            asTracking: false);

        var service = await _applicationsRepository
            .GetAsync(searchServiceRequest, cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        if (service is null)
        {
            _logger.LogApplicationServiceNotFound(id);
            return service;
        }

        _logger.LogApplicationServiceGetByIdCompleted(service.Id, service.Name);

        return service;
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanAll)]
    [HttpPost]
    public async Task<ApplicationService> Create([FromBody] CreateOrUpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var createRequest = new CreateServiceRequest(
            Name: request.Name,
            Description: request.Description,
            RepositoryUrl: request.RepositoryUrl,
            ContactEmail: request.ContactEmail,
            CreatedBy: User.GetId());

        var result = await _applicationsService.AddAsync(
            request: createRequest,
            cancellationToken: cancellationToken);

        return result;
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanAll)]
    [HttpPut("{serviceId:guid}")]
    public async Task<ApplicationService?> Update(
        [FromRoute] Guid serviceId,
        [FromBody] CreateOrUpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = new UpdateServiceRequest(
            Id: new ServiceId(serviceId),
            Name: request.Name,
            Description: request.Description,
            RepositoryUrl: request.RepositoryUrl,
            ContactEmail: request.ContactEmail,
            UpdatedBy: User.GetId());

        var result = await _applicationsService.UpdateAsync(
            request: updateRequest,
            cancellationToken: cancellationToken);

        return result;
    }

    [Authorize(Policy = AuthorizationPolicyNames.CanAll)]
    [HttpDelete("{serviceId:guid}")]
    public async Task Delete(Guid serviceId, CancellationToken cancellationToken)
    {
        var deleteServiceRequest = new DeleteServiceRequest(
            Id: new ServiceId(serviceId),
            DeletedBy: User.GetId());

        await _applicationsService.DeleteAsync(deleteServiceRequest, cancellationToken);
    }
}
