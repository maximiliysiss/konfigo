using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konfigo.Infrastructure.Persistence.Configurations;

internal sealed class ConfigEntryConfiguration : IEntityTypeConfiguration<ConfigEntry>
{
    public void Configure(EntityTypeBuilder<ConfigEntry> builder)
    {
        builder.ToTable("config_entries");

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => new EntryId(x));

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder
            .Property(x => x.ConfigVersionId)
            .HasColumnName("config_version_id")
            .HasConversion(x => x.Value, x => new VersionId(x));

        builder.Property(x => x.Key).HasColumnName("key");
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.RawValue).HasColumnName("raw_value");
        builder.Property(x => x.ValueType).HasColumnName("value_type");
        builder.Property(x => x.EnumDefinition).HasColumnName("enum_definition");
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.GroupName).HasColumnName("group_name");
        builder.Property(x => x.GroupDescription).HasColumnName("group_description");
        builder.Property(x => x.Generation).HasColumnName("generation");

        builder
            .HasOne(x => x.ConfigVersion)
            .WithMany(x => x.ConfigEntries)
            .HasForeignKey(x => x.ConfigVersionId);
    }
}
