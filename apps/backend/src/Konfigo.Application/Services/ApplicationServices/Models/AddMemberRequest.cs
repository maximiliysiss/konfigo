using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.ApplicationServices.Models;

public sealed record AddMemberRequest(ServiceId Id, UserId UserId, User CreatedBy);
