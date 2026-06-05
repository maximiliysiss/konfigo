namespace Konfigo.Client.Entities;

[StronglyTypedId(
    backingType: StronglyTypedIdBackingType.String,
    jsonConverter: StronglyTypedIdJsonConverter.SystemTextJson | StronglyTypedIdJsonConverter.SystemTextJson)]
internal readonly partial struct VersionId;
