using System;
using System.Collections.Generic;
using System.Linq;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Konfigo.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationServiceConfiguration : IEntityTypeConfiguration<ApplicationService>
{
    public void Configure(EntityTypeBuilder<ApplicationService> builder)
    {
        builder.ToTable("application_services");

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => new ServiceId(x));

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.RepositoryUrl).HasColumnName("repository_url");
        builder.Property(x => x.ContactEmail).HasColumnName("contact_email");

        var converter = new ValueConverter<HashSet<UserId>, string[]>(
            v => v.Select(x => x.Value).ToArray(),
            v => v.Select(x => new UserId(x)).ToHashSet());

        var comparer = new ValueComparer<HashSet<UserId>>(
            (a, b) => a!.SetEquals(b!),
            v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.Value.GetHashCode())),
            v => v.ToHashSet());

        builder
            .Property(x => x.Members)
            .HasColumnName("members")
            .HasConversion(converter, comparer)
            .HasColumnType("text[]");

        builder
            .Property(x => x.Num)
            .HasColumnName("num")
            .UseIdentityAlwaysColumn()
            .ValueGeneratedOnAdd()
            .DoNotSaveChanges();

        builder
            .HasMany(x => x.ConfigVersions)
            .WithOne()
            .HasForeignKey(x => x.ServiceId);
    }
}
