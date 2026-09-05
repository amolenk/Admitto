using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EventEmailContextViewEntityConfiguration
    : IEntityTypeConfiguration<EventEmailContextView>
{
    public void Configure(EntityTypeBuilder<EventEmailContextView> builder)
    {
        builder.ToTable("event_email_context_view");
        builder.HasKey(e => new { e.TeamId, e.TicketedEventId });

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("ticketed_event_id")
            .IsRequired();

        builder.Property(e => e.EventName)
            .HasColumnName("event_name")
            .HasMaxLength(EventName.MaxLength);

        builder.Property(e => e.WebsiteUrl)
            .HasColumnName("website_url")
            .HasColumnType("text");

        builder.Property(e => e.PublicSlug)
            .HasColumnName("public_slug")
            .HasMaxLength(Slug.MaxLength);

        builder.Property(e => e.TimeZone)
            .HasColumnName("time_zone")
            .HasMaxLength(TimeZoneId.MaxLength);

        builder.Property(e => e.ReconfirmOpensAt)
            .HasColumnName("reconfirm_opens_at")
            .HasColumnType("timestamptz");

        builder.Property(e => e.ReconfirmClosesAt)
            .HasColumnName("reconfirm_closes_at")
            .HasColumnType("timestamptz");

        builder.Property(e => e.ReconfirmMinEmailIntervalHours)
            .HasColumnName("reconfirm_min_email_interval_hours");

        builder.Property(e => e.ReconfirmQuietHoursStart)
            .HasColumnName("reconfirm_quiet_hours_start")
            .HasColumnType("time");

        builder.Property(e => e.ReconfirmQuietHoursEnd)
            .HasColumnName("reconfirm_quiet_hours_end")
            .HasColumnType("time");

        builder.Property(e => e.SelfServiceTicketTypeCount)
            .HasColumnName("self_service_ticket_type_count");

        builder.Property(e => e.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired();

        builder.Property(e => e.TicketedEventVersion)
            .HasColumnName("ticketed_event_version")
            .IsRequired();

        builder.Property(e => e.TicketCatalogVersion)
            .HasColumnName("ticket_catalog_version")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.LastUpdatedAt)
            .HasColumnName("last_updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(e => e.TeamId)
            .HasDatabaseName("IX_event_email_context_view_team_id");

        builder.HasIndex(e => new { e.IsArchived, e.ReconfirmOpensAt, e.ReconfirmClosesAt })
            .HasDatabaseName("IX_event_email_context_view_reconfirm_schedule");
    }
}
