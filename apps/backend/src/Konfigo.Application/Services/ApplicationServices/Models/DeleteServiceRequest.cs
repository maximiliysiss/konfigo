using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.ApplicationServices.Models;

public sealed record DeleteServiceRequest(ServiceId Id, UserId DeletedBy);
