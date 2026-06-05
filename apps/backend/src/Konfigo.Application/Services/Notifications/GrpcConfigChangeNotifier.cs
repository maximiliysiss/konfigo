using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Extensions;
using Konfigo.Application.Services.Notifications.Models;
using Konfigo.Application.Services.Notifications.Outbox.Grcp;
using Microsoft.Extensions.Logging;
using Publo.Abstraction.Services;

namespace Konfigo.Application.Services.Notifications;

internal sealed class GrpcConfigChangeNotifier(IPubloService outboxService, ILogger<GrpcConfigChangeNotifier> logger) : IConfigChangeNotifier
{
    public async Task HandleAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        if (request.Requests is [])
        {
            logger.LogOutboxSendSkippedEmptyRequest(request.ServiceId, request.VersionId);

            return;
        }

        logger.LogOutboxSendStarted(request.ServiceId, request.VersionId, request.Requests.Length);

        var requests = request.Requests
            .Select(Map)
            .ToArray();

        var grpcEvent = new GrpcEvent(request.ServiceId, request.VersionId, requests);

        await outboxService.SendAsync(
            message: grpcEvent,
            cancellationToken: cancellationToken);

        logger.LogOutboxSent(request.ServiceId, request.VersionId, requests.Length);

        return;

        static GrpcEvent.Request Map(NotificationRequest.Request c)
        {
            return new GrpcEvent.Request(
                EntryId: c.EntryId,
                Key: c.Key,
                RawValue: c.RawValue,
                Generation: c.Generation,
                Timestamp: c.Timestamp);
        }
    }
}
