namespace Konfigo.Domain.ValueType;

[StronglyTypedId(
    backingType: StronglyTypedIdBackingType.Guid,
    jsonConverter: StronglyTypedIdJsonConverter.SystemTextJson)]
public readonly partial struct EntryId;
