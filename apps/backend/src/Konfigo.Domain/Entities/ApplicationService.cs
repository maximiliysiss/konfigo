using System;
using System.Collections.Generic;
using Konfigo.Domain.Shared;
using Konfigo.Domain.ValueType;

namespace Konfigo.Domain.Entities;

public sealed class ApplicationService : EntityBase<ServiceId>
{
    public int Num { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? GitLabProjectId { get; set; }
    public string? ContactEmail { get; set; }
    public ICollection<ConfigVersion> ConfigVersions { get; set; } = [];

    public void Update(UpdateServiceRequest request, DateTimeOffset now)
    {
        Name = request.Name;
        Description = request.Description;
        RepositoryUrl = request.RepositoryUrl;
        GitLabProjectId = request.GitLabProjectId;
        ContactEmail = request.ContactEmail;
        UpdatedAt = now;
    }
}
