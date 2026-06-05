using System.Threading.Tasks;
using Konfigo.Domain.ValueType;
using Konfigo.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace Konfigo.Hubs;

public class RealtimeConfigHub : Hub
{
    public Task JoinVersionGroup(ServiceId serviceId, VersionId versionId)
    {
        var group = (serviceId, versionId).AsSignalKey();
        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public Task LeaveVersionGroup(ServiceId serviceId, VersionId versionId)
    {
        var group = (serviceId, versionId).AsSignalKey();
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }
}
