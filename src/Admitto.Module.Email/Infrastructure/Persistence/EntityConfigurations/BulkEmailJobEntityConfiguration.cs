using System.Text.Json;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class BulkEmailJobEntityConfiguration : IEntityTypeConfiguration<BulkEmailJob>
{
    private static readonly JsonSerializerOptions SourceJsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<BulkEmailJob> builder)
    {
        builder.ToTable("bulk_email_jobs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => BulkEmailJobId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .HasConversion(v => v.Value, v => TeamId.From(v))
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("ticketed_event_id")
            .HasConversion(v => v.Value, v => TicketedEventId.From(v))
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

        var sourceConverter = new ValueConverter<BulkEmailJobSource, string>(
            v => JsonSerializer.Serialize(v, SourceJsonOptions),
            v => JsonSerializer.Deserialize<BulkEmailJobSource>(v, SourceJsonOptions)!);

        builder.Property(e => e.Source)
            .HasColumnName("source")
            .HasColumnType("jsonb")
            .HasConversion(sourceConverter)
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
                .HasJsonPropertyName("display_name");

            b.Property(r => r.RegistrationId)
                .HasJsonPropertyName("registration_id");

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
    }
}
