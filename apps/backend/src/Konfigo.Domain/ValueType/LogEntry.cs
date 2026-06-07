using System.Text.Json.Serialization;
using Konfigo.Domain.Enums;

namespace Konfigo.Domain.ValueType;

[JsonPolymorphic]
[JsonDerivedType(typeof(ServiceCreatedEntry), "ServiceCreated")]
[JsonDerivedType(typeof(ServiceUpdatedEntry), "ServiceUpdated")]
[JsonDerivedType(typeof(ServiceMemberAddedEntry), "ServiceMemberAdded")]
[JsonDerivedType(typeof(ServiceMemberRemovedEntry), "ServiceMemberRemoved")]
[JsonDerivedType(typeof(ServiceDeletedEntry), "ServiceDeleted")]
[JsonDerivedType(typeof(VersionCreatedEntry), "VersionCreated")]
[JsonDerivedType(typeof(VersionUpdatedEntry), "VersionUpdated")]
[JsonDerivedType(typeof(EntryCreatedEntry), "EntryCreated")]
[JsonDerivedType(typeof(EntryUpdatedEntry), "EntryUpdated")]
[JsonDerivedType(typeof(EntryDeletedEntry), "EntryDeleted")]
[JsonDerivedType(typeof(EntrySetEntry), "EntrySet")]
public abstract record LogEntry(LogType Type);

public sealed record ServiceCreatedEntry(
    string Name,
    string? Description,
    string? RepositoryUrl,
    string? ContactEmail) : LogEntry(LogType.ServiceCreated);

public sealed record ServiceUpdatedEntry(
    string Name,
    string? Description,
    string? RepositoryUrl,
    string? ContactEmail) : LogEntry(LogType.ServiceUpdated);

public sealed record ServiceMemberAddedEntry(UserId UserId) : LogEntry(LogType.ServiceMemberAdded);

public sealed record ServiceMemberRemovedEntry(UserId UserId) : LogEntry(LogType.ServiceMemberRemoved);

public sealed record ServiceDeletedEntry(
    string Name,
    string? Description,
    string? RepositoryUrl,
    string? ContactEmail) : LogEntry(LogType.ServiceDeleted);

public sealed record VersionCreatedEntry(
    VersionId Id,
    string VersionLabel,
    string? Description) : LogEntry(LogType.VersionCreated);

public sealed record VersionUpdatedEntry(
    VersionId Id,
    string VersionLabel,
    string? Description) : LogEntry(LogType.VersionUpdated);

public sealed record EntryCreatedEntry(
    EntryId Id,
    string? RawValue,
    string? EnumDefinition,
    string? Description,
    string? GroupName,
    string? GroupDescription) : LogEntry(LogType.EntryCreated);

public sealed record EntryUpdatedEntry(
    EntryId Id,
    string? RawValue,
    string? EnumDefinition,
    string? Description,
    string? GroupName,
    string? GroupDescription) : LogEntry(LogType.EntryUpdated);

public sealed record EntryDeletedEntry(
    EntryId Id,
    string? RawValue,
    string? EnumDefinition,
    string? Description,
    string? GroupName,
    string? GroupDescription) : LogEntry(LogType.EntryDeleted);

public sealed record EntrySetEntry(EntryId Id, string? Value) : LogEntry(LogType.EntrySet);
