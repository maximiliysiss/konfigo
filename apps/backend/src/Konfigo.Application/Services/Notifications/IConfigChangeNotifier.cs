using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.Notifications.Models;

namespace Konfigo.Application.Services.Notifications;

public interface IConfigChangeNotifier
{
    Task HandleAsync(NotificationRequest request, CancellationToken cancellationToken);
}
