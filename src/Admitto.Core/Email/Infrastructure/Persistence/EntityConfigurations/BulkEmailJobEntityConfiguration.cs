using System.Text.Json;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class BulkEmailJobEntityConfiguration : IEntityTypeConfiguration<BulkEmailJob>
{
    private static readonly JsonSerializerOptions FilterJsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<BulkEmailJob> builder)
    {
        builder.ToTable("bulk_email_jobs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("ticketed_event_id")
            .IsRequired();

        builder.Property(e => e.EmailType)
            .HasColumnName("email_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(500);

        builder.Property(e => e.TextBody)
            .HasColumnName("text_body")
            .HasColumnType("text");

        builder.Property(e => e.HtmlBody)
            .HasColumnName("html_body")
            .HasColumnType("text");

        var filterConverter = new ValueConverter<BulkEmailAttendeeFilter, string>(
            v => JsonSerializer.Serialize(v, FilterJsonOptions),
            v => JsonSerializer.Deserialize<BulkEmailAttendeeFilter>(v, FilterJsonOptions)!);

        builder.Property(e => e.AttendeeFilter)
            .HasColumnName("attendee_filter")
            .HasColumnType("jsonb")
            .HasConversion(filterConverter)
            .IsRequired();

        builder.Property(e => e.TriggeredBy)
            .HasColumnName("triggered_by")
            .HasMaxLength(EmailAddress.MaxLength);

        builder.Property(e => e.IsSystemTriggered)
            .HasColumnName("is_system_triggered")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.RecipientCount)
            .HasColumnName("recipient_count")
            .IsRequired();

        builder.Property(e => e.SentCount)
            .HasColumnName("sent_count")
            .IsRequired();

        builder.Property(e => e.FailedCount)
            .HasColumnName("failed_count")
            .IsRequired();

        builder.Property(e => e.CancelledCount)
            .HasColumnName("cancelled_count")
            .IsRequired();

        builder.Property(e => e.LastError)
            .HasColumnName("last_error")
            .HasColumnType("text");

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamptz");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamptz");

        builder.Property(e => e.CancellationRequestedAt)
            .HasColumnName("cancellation_requested_at")
            .HasColumnType("timestamptz");

        builder.Property(e => e.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamptz");

        builder.OwnsMany(e => e.Recipients, b =>
        {
            b.ToJson("recipients");

            b.Property(r => r.Email)
                .HasJsonPropertyName("email")
                .IsRequired();

            b.Property(r => r.DisplayName)
                .HasJsonPropertyName("display_name")
                .IsRequired();

            b.Property(r => r.RegistrationId)
                .HasJsonPropertyName("registration_id")
                .IsRequired();

            b.Property(r => r.ParametersJson)
                .HasJsonPropertyName("parameters")
                .IsRequired();

            b.Property(r => r.Status)
                .HasJsonPropertyName("status")
                .HasConversion<string>()
                .IsRequired();

            b.Property(r => r.LastError)
                .HasJsonPropertyName("last_error");
        });

        builder.HasIndex(e => new { e.TicketedEventId, e.CreatedAt })
            .HasDatabaseName("IX_bulk_email_jobs_event_created_at")
            .IsDescending(false, true);

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_bulk_email_jobs_status");

        // A system reconfirm job is the durable reservation for an event's
        // current hourly evaluation. The partial unique index closes the race
        // between two evaluators that both observe no pending job.
        builder.HasIndex(e => new { e.TicketedEventId, e.EmailType })
            .HasDatabaseName("IX_bulk_email_jobs_active_reconfirm_event")
            .IsUnique()
            .HasFilter("is_system_triggered = TRUE AND email_type = 'Reconfirmation' AND status IN ('Pending', 'Resolving', 'Sending')");
    }
}
