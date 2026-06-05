using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Updater.Models;

public sealed record CreateSubscriberRequest(ServiceId ServiceId, VersionId VersionId);
