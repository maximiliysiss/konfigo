using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Updater.Models;

public sealed record DeleteRequest(ServiceId ServiceId, VersionId VersionId, Subscriber Subscriber);
