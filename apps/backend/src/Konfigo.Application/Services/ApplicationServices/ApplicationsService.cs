using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Extensions;
using Konfigo.Application.Infrastructure.DateTime;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.ApplicationServices.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Microsoft.Extensions.Logging;
using UpdateServiceRequest = Konfigo.Application.Services.ApplicationServices.Models.UpdateServiceRequest;

namespace Konfigo.Application.Services.ApplicationServices;

internal sealed class ApplicationsService : IApplicationsService
{
    private readonly IApplicationsRepository _repository;

    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly ILogger<ApplicationsService> _logger;

    public ApplicationsService(
        IApplicationsRepository repository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ApplicationsService> logger)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<ApplicationService> AddAsync(CreateServiceRequest request, CancellationToken cancellationToken)
    {
        _logger.LogApplicationServiceCreateStarted(request.Name);

        var applicationService = new ApplicationService
        {
            Id = ServiceId.New(),
            Name = request.Name,
            Description = request.Description,
            RepositoryUrl = request.RepositoryUrl,
            GitLabProjectId = request.GitLabProjectId,
            ContactEmail = request.ContactEmail,
            CreatedAt = _dateTimeProvider.GetNow(),
        };

        applicationService = await _repository.AddAsync(applicationService, cancellationToken);

        _logger.LogApplicationServiceCreated(applicationService.Id, applicationService.Name);

        return applicationService;
    }

    public async Task<ApplicationService?> UpdateAsync(UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        _logger.LogApplicationServiceUpdateStarted(request.Id, request.Name);

        var service = await _repository
            .GetAsync(SearchServiceRequest.Create(ids: [request.Id]), cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        if (service is null)
        {
            _logger.LogApplicationServiceNotFound(request.Id);
            return null;
        }

        var updateServiceRequest = new Domain.ValueType.UpdateServiceRequest(
            Name: request.Name,
            Description: request.Description,
            RepositoryUrl: request.RepositoryUrl,
            GitLabProjectId: request.GitLabProjectId,
            ContactEmail: request.ContactEmail);

        service.Update(updateServiceRequest, _dateTimeProvider.GetNow());

        await _repository.UpdateAsync(service, cancellationToken);

        _logger.LogApplicationServiceUpdated(service.Id, service.Name);

        return service;
    }

    public async Task<ApplicationService?> DeleteAsync(DeleteServiceRequest request, CancellationToken cancellationToken)
    {
        _logger.LogApplicationServiceDeleteStarted(request.Id);

        var service = await _repository
            .GetAsync(SearchServiceRequest.Create(ids: [request.Id]), cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        if (service is null)
        {
            _logger.LogApplicationServiceNotFound(request.Id);
            return null;
        }

        await _repository.DeleteAsync(service, cancellationToken);

        _logger.LogApplicationServiceDeleted(service.Id, service.Name);

        return service;
    }
}
