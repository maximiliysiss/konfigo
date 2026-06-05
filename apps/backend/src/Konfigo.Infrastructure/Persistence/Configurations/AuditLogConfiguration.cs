using System.Text.Json;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konfigo.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
    };

    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => new LogId(x));

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder
            .Property(x => x.ServiceId)
            .HasColumnName("service_id")
            .HasConversion(x => x.Value, x => new ServiceId(x));

        builder
            .Property(x => x.Entry)
            .HasColumnName("entry")
            .HasColumnType("jsonb")
            .HasConversion(
                x => JsonSerializer.Serialize(x, _options),
                x => JsonSerializer.Deserialize<LogEntry>(x, _options)!);

        builder
            .Property(x => x.Num)
            .HasColumnName("num")
            .UseIdentityAlwaysColumn()
            .ValueGeneratedOnAdd()
            .DoNotSaveChanges();

        builder
            .Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasConversion<string?>(
                x => x.HasValue ? x.Value.Value : null,
                x => x == null ? null : new UserId(x));
    }
}
