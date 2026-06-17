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

        builder.Ignore(x => x.Members);

        builder
            .PrimitiveCollection<List<string>>("_members")
            .HasColumnName("members")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
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
