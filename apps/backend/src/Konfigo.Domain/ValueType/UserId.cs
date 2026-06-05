namespace Konfigo.Domain.ValueType;

[StronglyTypedId(
    backingType: StronglyTypedIdBackingType.String,
    jsonConverter: StronglyTypedIdJsonConverter.SystemTextJson)]
public readonly partial struct UserId;
