using System;
using System.Collections.Generic;
using System.Linq;
using Konfigo.Domain.Shared;
using Konfigo.Domain.ValueType;

namespace Konfigo.Domain.Entities;

public sealed class ApplicationService : EntityBase<ServiceId>
{
    public int Num { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? ContactEmail { get; set; }
    public HashSet<UserId> Members { get; set; } = [];
    public ICollection<ConfigVersion> ConfigVersions { get; set; } = [];

    public void Update(UpdateServiceRequest request, DateTimeOffset now)
    {
        Name = request.Name;
        Description = request.Description;
        RepositoryUrl = request.RepositoryUrl;
        ContactEmail = request.ContactEmail;
        UpdatedAt = now;
    }

    public bool TryAddMember(UserId userId, DateTimeOffset now)
    {
        if (!Members.Add(userId))
            return false;

        UpdatedAt = now;

        return true;
    }

    public bool TryRemoveMember(UserId userId, DateTimeOffset now)
    {
        if (!Members.Remove(userId))
            return false;

        UpdatedAt = now;

        return true;
    }
}
