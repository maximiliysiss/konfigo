using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Konfigo.Application.Infrastructure.DateTime;
using Konfigo.Application.Repositories;
using Konfigo.Application.Services.ApplicationServices.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using UpdateServiceRequest = Konfigo.Application.Services.ApplicationServices.Models.UpdateServiceRequest;

namespace Konfigo.Application.Services.ApplicationServices.Audit;

internal sealed class AuditApplicationsService : IApplicationsService
{
    private readonly IApplicationsService _service;

    private readonly IAuditLogRepository _auditLogRepository;

    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditApplicationsService(
        IApplicationsService service,
        IAuditLogRepository auditLogRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _service = service;
        _auditLogRepository = auditLogRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationService> AddAsync(CreateServiceRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var applicationService = await _service.AddAsync(request, cancellationToken);

        var auditLog = new AuditLog
        {
            Entry = new ServiceCreatedEntry(
                Name: request.Name,
                Description: request.Description,
                RepositoryUrl: request.RepositoryUrl,
                ContactEmail: request.ContactEmail),
            ServiceId = applicationService.Id,
            UserId = request.CreatedBy,
            Id = LogId.New(),
            CreatedAt = applicationService.CreatedAt,
        };

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        transactionScope.Complete();

        return applicationService;
    }

    public async Task<ApplicationService?> UpdateAsync(UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var applicationService = await _service.UpdateAsync(request, cancellationToken);

        if (applicationService is not null)
        {
            var auditLog = new AuditLog
            {
                ServiceId = applicationService.Id,
                UserId = request.UpdatedBy,
                Id = LogId.New(),
                CreatedAt = applicationService.UpdatedAt ?? applicationService.CreatedAt,
                Entry = new ServiceUpdatedEntry(
                    Name: request.Name,
                    Description: request.Description,
                    RepositoryUrl: request.RepositoryUrl,
                    ContactEmail: request.ContactEmail)
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }

        transactionScope.Complete();

        return applicationService;
    }

    public async Task<ApplicationService?> DeleteAsync(DeleteServiceRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var service = await _service.DeleteAsync(request, cancellationToken);

        if (service is not null)
        {
            var auditLog = new AuditLog
            {
                Id = LogId.New(),
                Entry = new ServiceDeletedEntry(
                    Name: service.Name,
                    Description: service.Description,
                    RepositoryUrl: service.RepositoryUrl,
                    ContactEmail: service.ContactEmail),
                ServiceId = request.Id,
                UserId = request.DeletedBy,
                CreatedAt = _dateTimeProvider.GetNow(),
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }

        transactionScope.Complete();

        return service;
    }
}
