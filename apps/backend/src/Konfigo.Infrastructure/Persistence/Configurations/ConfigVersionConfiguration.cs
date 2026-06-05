using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konfigo.Infrastructure.Persistence.Configurations;

internal sealed class ConfigVersionConfiguration : IEntityTypeConfiguration<ConfigVersion>
{
    public void Configure(EntityTypeBuilder<ConfigVersion> builder)
    {
        builder.ToTable("config_versions");

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => new VersionId(x));

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder
            .Property(x => x.ServiceId)
            .HasColumnName("service_id")
            .HasConversion(x => x.Value, x => new ServiceId(x));

        builder.Property(x => x.VersionLabel).HasColumnName("version_label");
        builder.Property(x => x.Description).HasColumnName("description");

        builder
            .HasOne<ApplicationService>()
            .WithMany(x => x.ConfigVersions)
            .HasForeignKey(x => x.ServiceId);

        builder
            .HasMany(x => x.ConfigEntries)
            .WithOne(x => x.ConfigVersion)
            .HasForeignKey(x => x.ConfigVersionId);
    }
}
