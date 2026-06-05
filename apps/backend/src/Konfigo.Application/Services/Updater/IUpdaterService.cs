using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.Updater.Models;

namespace Konfigo.Application.Services.Updater;

public interface IUpdaterService
{
    ValueTask<Subscriber> CreateAsync(CreateSubscriberRequest request, CancellationToken cancellationToken);
    ValueTask PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken);
}
