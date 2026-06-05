using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Extensions;
using Konfigo.Application.Services.Updater;
using Konfigo.Application.Services.Updater.Models;
using Microsoft.Extensions.Logging;
using Publo.Abstraction.Executor;

namespace Konfigo.Application.Services.Notifications.Outbox.Grcp;

internal sealed class GrpcEventExecutor(IUpdaterService updaterService, ILogger<GrpcEventExecutor> logger) : IPubloExecutor<GrpcEvent>
{
    public async Task HandleAsync(GrpcEvent message, CancellationToken cancellationToken)
    {
        logger.LogOutboxExecuteStarted(message.ServiceId, message.VersionId, message.Requests.Length);

        var requests = message.Requests
            .Select(Map)
            .ToArray();

        var changeEvent = new ChangeEvent(
            ServiceId: message.ServiceId,
            VersionId: message.VersionId,
            Requests: requests);

        await updaterService.PublishAsync(
            changeEvent: changeEvent,
            cancellationToken: CancellationToken.None);

        logger.LogOutboxExecuted(message.ServiceId, message.VersionId, requests.Length);

        return;

        static ChangeEvent.Request Map(GrpcEvent.Request c)
        {
            return new ChangeEvent.Request(
                EntryId: c.EntryId,
                Key: c.Key,
                RawValue: c.RawValue,
                Generation: c.Generation,
                Timestamp: c.Timestamp);
        }
    }
}
