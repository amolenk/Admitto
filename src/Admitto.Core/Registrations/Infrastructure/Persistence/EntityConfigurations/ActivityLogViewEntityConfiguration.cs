using Amolenk.Admitto.Core.Registrations.Application.Projections.ActivityLog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.EntityConfigurations;

internal sealed class ActivityLogViewEntityConfiguration : IEntityTypeConfiguration<ActivityLogView>
{
    public void Configure(EntityTypeBuilder<ActivityLogView> builder)
    {
        builder.ToTable("activity_log_view");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        builder.Property(e => e.ActivityType)
            .HasColumnName("activity_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("text");

        builder.HasIndex(e => new { e.TeamId, e.EventId, e.RegistrationId, e.ActivityType, e.OccurredAt })
            .HasDatabaseName("IX_activity_log_view_registration_type_occurred");
    }
}
