using System;
using System.Linq;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.ValueType;
using Konfigo.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Konfigo.Authorization;

internal sealed class ServiceAccessActionFilter : IAsyncActionFilter
{
    private readonly IApplicationsRepository _repository;

    private readonly ILogger<ServiceAccessActionFilter> _logger;

    private readonly KonfigoAuthenticationOptions _options;

    public ServiceAccessActionFilter(
        IApplicationsRepository repository,
        ILogger<ServiceAccessActionFilter> logger,
        IOptions<KonfigoAuthenticationOptions> options)
    {
        _repository = repository;
        _logger = logger;
        _options = options.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        const string serviceIdKey = "serviceId";

        if (context.RouteData.Values.TryGetValue(serviceIdKey, out var value))
        {
            Guid? serviceId = value switch
            {
                string str when Guid.TryParse(str, out var guid) => guid,
                Guid guid => guid,
                _ => null
            };

            if (serviceId is null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            var id = new ServiceId(serviceId.Value);

            _logger.LogServiceAccessCheckStarted(id);

            var service = await _repository
                .GetAsync(SearchServiceRequest.Create(ids: [id]), context.HttpContext.RequestAborted)
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

            if (service is null)
            {
                _logger.LogApplicationServiceNotFound(id);
                context.Result = new NotFoundResult();
                return;
            }

            var userId = context.HttpContext.User.GetMemberId(_options);

            if (userId is not null && !service.Members.Contains(userId.Value))
            {
                _logger.LogAccessDenied(id);
                context.Result = new ForbidResult();
                return;
            }

            _logger.LogServiceAccessCheckCompleted(id, service.Name);
        }

        await next();
    }
}
