using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

internal sealed class ActivityLogEntityConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_log");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => ActivityLogId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        builder.Property(e => e.ActivityType)
            .HasColumnName("activity_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("text");

        builder.HasIndex(e => new { e.RegistrationId, e.ActivityType, e.OccurredAt })
            .HasDatabaseName("IX_activity_log_registration_type_occurred");
    }
}
