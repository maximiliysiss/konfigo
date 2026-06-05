using System;
using AutoBogus;
using Konfigo.Application.Services.ApplicationServices.Models;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.Enums;
using Konfigo.Domain.ValueType;
using AppUpdateEntryRequest = Konfigo.Application.Services.Configurations.Models.UpdateEntryRequest;
using AppUpdateServiceRequest = Konfigo.Application.Services.ApplicationServices.Models.UpdateServiceRequest;
using AppUpdateVersionRequest = Konfigo.Application.Services.Configurations.Models.UpdateVersionRequest;

namespace Konfigo.UnitTests.Support;

internal static class TestFakes
{
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    public static ApplicationService BuildService(
        ServiceId? id = null,
        string? name = null,
        string? description = null,
        string? repositoryUrl = null,
        string? gitLabProjectId = null,
        string? contactEmail = null)
    {
        var service = AutoFaker.Generate<ApplicationService>();

        service.Id = id ?? ServiceId.New();
        service.Name = name ?? service.Name;
        service.Description = description ?? service.Description;
        service.RepositoryUrl = repositoryUrl ?? service.RepositoryUrl;
        service.GitLabProjectId = gitLabProjectId ?? service.GitLabProjectId;
        service.ContactEmail = contactEmail ?? service.ContactEmail;
        service.CreatedAt = Now;
        service.ConfigVersions = [];

        return service;
    }

    public static ConfigVersion BuildVersion(
        VersionId? id = null,
        ServiceId? serviceId = null,
        string? versionLabel = null,
        string? description = null)
    {
        var version = AutoFaker.Generate<ConfigVersion>();

        version.Id = id ?? VersionId.New();
        version.ServiceId = serviceId ?? ServiceId.New();
        version.VersionLabel = versionLabel ?? version.VersionLabel;
        version.Description = description ?? version.Description;
        version.CreatedAt = Now;
        version.ConfigEntries = [];

        return version;
    }

    public static ConfigEntry BuildEntry(
        EntryId? id = null,
        VersionId? versionId = null,
        string? key = null,
        string? name = null,
        string? rawValue = null,
        ConfigValueType valueType = ConfigValueType.String,
        int generation = 1)
    {
        var entry = AutoFaker.Generate<ConfigEntry>();

        entry.Id = id ?? EntryId.New();
        entry.ConfigVersionId = versionId ?? VersionId.New();
        entry.Key = key ?? entry.Key;
        entry.Name = name ?? entry.Name;
        entry.RawValue = rawValue ?? entry.RawValue;
        entry.ValueType = valueType;
        entry.EnumDefinition = null;
        entry.Generation = generation;
        entry.CreatedAt = Now;

        return entry;
    }

    public static CreateEntryRequest BuildCreateEntryRequest(
        ServiceId? serviceId = null,
        VersionId? versionId = null,
        UserId? createdBy = null,
        string? key = null,
        string? name = null,
        string? rawValue = null,
        ConfigValueType valueType = ConfigValueType.String)
    {
        var request = AutoFaker.Generate<CreateEntryRequest>();

        return request with
        {
            ServiceId = serviceId ?? ServiceId.New(),
            VersionId = versionId ?? VersionId.New(),
            Key = key ?? request.Key,
            Name = name ?? request.Name,
            RawValue = rawValue ?? request.RawValue,
            ValueType = valueType,
            EnumDefinition = null,
            CreatedBy = createdBy ?? BuildUserId(),
        };
    }

    public static AppUpdateEntryRequest BuildUpdateEntryRequest(
        ServiceId? serviceId = null,
        VersionId? versionId = null,
        EntryId? id = null,
        string? rawValue = null,
        int generation = 1,
        UserId? updatedBy = null)
    {
        var request = AutoFaker.Generate<AppUpdateEntryRequest>();

        return request with
        {
            ServiceId = serviceId ?? ServiceId.New(),
            VersionId = versionId ?? VersionId.New(),
            Id = id ?? EntryId.New(),
            RawValue = rawValue ?? request.RawValue,
            EnumDefinition = null,
            Generation = generation,
            UpdatedBy = updatedBy ?? BuildUserId(),
        };
    }

    public static DeleteEntryRequest BuildDeleteEntryRequest(
        ServiceId? serviceId = null,
        VersionId? versionId = null,
        EntryId? id = null,
        UserId? deletedBy = null)
    {
        var request = AutoFaker.Generate<DeleteEntryRequest>();

        return request with
        {
            ServiceId = serviceId ?? ServiceId.New(),
            VersionId = versionId ?? VersionId.New(),
            Id = id ?? EntryId.New(),
            DeletedBy = deletedBy ?? BuildUserId(),
        };
    }

    public static CreateVersionRequest BuildCreateVersionRequest(
        ServiceId? serviceId = null,
        string? versionLabel = null,
        string? description = null,
        UserId? createdBy = null)
    {
        var request = AutoFaker.Generate<CreateVersionRequest>();

        return request with
        {
            ServiceId = serviceId ?? ServiceId.New(),
            VersionLabel = versionLabel ?? request.VersionLabel,
            Description = description ?? request.Description,
            CreatedBy = createdBy ?? BuildUserId(),
        };
    }

    public static AppUpdateVersionRequest BuildUpdateVersionRequest(
        ServiceId? serviceId = null,
        VersionId? versionId = null,
        string? versionLabel = null,
        string? description = null,
        UserId? updatedBy = null)
    {
        var request = AutoFaker.Generate<AppUpdateVersionRequest>();

        return request with
        {
            ServiceId = serviceId ?? ServiceId.New(),
            VersionId = versionId ?? VersionId.New(),
            VersionLabel = versionLabel ?? request.VersionLabel,
            Description = description ?? request.Description,
            UpdatedBy = updatedBy ?? BuildUserId(),
        };
    }

    public static GenerateVersionRequest BuildGenerateVersionRequest(
        ServiceId? serviceId = null,
        string? versionLabel = null,
        params GenerateVersionRequest.EntryRequest[] entries)
    {
        var request = AutoFaker.Generate<GenerateVersionRequest>();

        return request with
        {
            ServiceId = serviceId ?? ServiceId.New(),
            VersionLabel = versionLabel ?? request.VersionLabel,
            Entries = entries,
        };
    }

    public static GenerateVersionRequest.EntryRequest BuildGenerateEntryRequest(
        string? key = null,
        string? name = null,
        string? rawValue = null,
        ConfigValueType valueType = ConfigValueType.String)
    {
        var request = AutoFaker.Generate<GenerateVersionRequest.EntryRequest>();

        return request with
        {
            Key = key ?? request.Key,
            Name = name ?? request.Name,
            RawValue = rawValue ?? request.RawValue,
            ValueType = valueType,
            EnumDefinition = null,
        };
    }

    public static CreateServiceRequest BuildCreateServiceRequest(
        string? name = null,
        UserId? createdBy = null)
    {
        var request = AutoFaker.Generate<CreateServiceRequest>();

        return request with
        {
            Name = name ?? request.Name,
            CreatedBy = createdBy ?? BuildUserId(),
        };
    }

    public static AppUpdateServiceRequest BuildUpdateServiceRequest(
        ServiceId? id = null,
        string? name = null,
        UserId? updatedBy = null)
    {
        var request = AutoFaker.Generate<AppUpdateServiceRequest>();

        return request with
        {
            Id = id ?? ServiceId.New(),
            Name = name ?? request.Name,
            UpdatedBy = updatedBy ?? BuildUserId(),
        };
    }

    public static DeleteServiceRequest BuildDeleteServiceRequest(
        ServiceId? id = null,
        UserId? deletedBy = null)
    {
        var request = AutoFaker.Generate<DeleteServiceRequest>();

        return request with
        {
            Id = id ?? ServiceId.New(),
            DeletedBy = deletedBy ?? BuildUserId(),
        };
    }

    private static UserId BuildUserId() => new(Guid.NewGuid().ToString());
}
