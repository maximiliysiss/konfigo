using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.ApplicationServices.Models;

public sealed record RemoveMemberRequest(ServiceId Id, UserId UserId, UserId CreatedBy);
