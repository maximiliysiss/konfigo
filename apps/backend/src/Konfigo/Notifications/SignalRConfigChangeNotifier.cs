using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.Notifications;
using Konfigo.Application.Services.Notifications.Models;
using Konfigo.Extensions;
using Konfigo.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Konfigo.Notifications;

internal sealed class SignalRConfigChangeNotifier(IHubContext<RealtimeConfigHub> hubContext) : IConfigChangeNotifier
{
    public Task HandleAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            request.ServiceId,
            request.VersionId,
            Requests = request.Requests
                .Select(r => new { Id = r.EntryId.Value, Value = r.RawValue })
                .ToArray()
        };

        return hubContext.Clients
            .Group((request.ServiceId, request.VersionId).AsSignalKey())
            .SendAsync("ConfigChanged", payload, cancellationToken: cancellationToken);
    }
}
