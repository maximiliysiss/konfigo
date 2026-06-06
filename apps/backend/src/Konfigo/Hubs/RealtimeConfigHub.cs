using System.Threading.Tasks;
using Konfigo.Authorization;
using Konfigo.Domain.ValueType;
using Konfigo.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Konfigo.Hubs;

[Authorize(Policy = AuthorizationPolicyNames.CanChange)]
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
