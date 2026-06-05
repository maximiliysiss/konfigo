using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Extensions;
using Konfigo.Application.Services.Updater.Models;
using Konfigo.Domain.ValueType;
using Microsoft.Extensions.Logging;

namespace Konfigo.Application.Services.Updater;

internal sealed class UpdaterService(ILogger<UpdaterService> logger) : IUpdaterService
{
    private readonly ConcurrentDictionary<(ServiceId, VersionId), ConcurrentDictionary<Subscriber, byte>> _subscribers = [];

    public ValueTask<Subscriber> CreateAsync(CreateSubscriberRequest request, CancellationToken cancellationToken)
    {
        var subscribers = _subscribers.GetOrAdd((request.ServiceId, request.VersionId), _ => []);
        logger.LogSubscriberCreateStarted(request.ServiceId, request.VersionId, subscribers.Count);

        var subscriber = new Subscriber(onDispose: s => DeleteAsync(CreateDeleteRequest(s), CancellationToken.None));

        if (!subscribers.TryAdd(subscriber, 0))
        {
            logger.LogSubscriberCreateDuplicate(request.ServiceId, request.VersionId, subscribers.Count);
        }

        logger.LogSubscriberCreated(request.ServiceId, request.VersionId, subscribers.Count);

        return ValueTask.FromResult(subscriber);

        DeleteRequest CreateDeleteRequest(Subscriber s) => new(request.ServiceId, request.VersionId, s);
    }

    public async ValueTask PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        if (!_subscribers.TryGetValue((changeEvent.ServiceId, changeEvent.VersionId), out var subscribers))
        {
            logger.LogSubscriberPublishNoSubscribers(
                changeEvent.ServiceId,
                changeEvent.VersionId,
                changeEvent.Requests.Length);

            return;
        }

        logger.LogSubscriberPublishStarted(
            changeEvent.ServiceId,
            changeEvent.VersionId,
            subscribers.Count,
            changeEvent.Requests.Length);

        foreach (var (subscriber, _) in subscribers)
        {
            await subscriber.PublishAsync(changeEvent, cancellationToken);
        }

        logger.LogSubscriberPublished(
            changeEvent.ServiceId,
            changeEvent.VersionId,
            subscribers.Count,
            changeEvent.Requests.Length);
    }

    private ValueTask DeleteAsync(DeleteRequest request, CancellationToken cancellationToken)
    {
        if (!_subscribers.TryGetValue((request.ServiceId, request.VersionId), out var subscribers))
        {
            logger.LogSubscriberDeleteNoSubscribers(request.ServiceId, request.VersionId);

            return ValueTask.CompletedTask;
        }

        if (!subscribers.TryRemove(request.Subscriber, out _))
        {
            logger.LogSubscriberDeleteFailed(request.ServiceId, request.VersionId, subscribers.Count);

            return ValueTask.CompletedTask;
        }

        logger.LogSubscriberDeleted(request.ServiceId, request.VersionId, subscribers.Count);

        return ValueTask.CompletedTask;
    }
}
