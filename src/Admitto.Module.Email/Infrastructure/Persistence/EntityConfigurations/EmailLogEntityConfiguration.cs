using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmailLogEntityConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_log");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => EmailLogId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("ticketed_event_id")
            .IsRequired();

        builder.Property(e => e.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Recipient)
            .HasColumnName("recipient")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(e => e.EmailType)
            .HasColumnName("email_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Provider)
            .HasColumnName("provider")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ProviderMessageId)
            .HasColumnName("provider_message_id")
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamptz");

        builder.Property(e => e.StatusUpdatedAt)
            .HasColumnName("status_updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.LastError)
            .HasColumnName("last_error")
            .HasColumnType("text");

        builder.Property(e => e.BulkEmailJobId)
            .HasColumnName("bulk_email_job_id")
            .HasConversion(
                v => v == null ? (Guid?)null : v.Value.Value,
                v => v == null ? (Domain.ValueObjects.BulkEmailJobId?)null : Domain.ValueObjects.BulkEmailJobId.From(v.Value));

        builder.HasOne<BulkEmailJob>()
            .WithMany()
            .HasForeignKey(e => e.BulkEmailJobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.TicketedEventId, e.Recipient, e.IdempotencyKey })
            .HasDatabaseName("IX_email_log_event_recipient_idempotency")
            .IsUnique();

        builder.HasIndex(e => new { e.TicketedEventId, e.SentAt })
            .HasDatabaseName("IX_email_log_event_sent_at")
            .IsDescending(false, true);

        builder.Property(e => e.RegistrationId)
            .HasColumnName("registration_id");

        builder.HasIndex(e => new { e.TicketedEventId, e.RegistrationId })
            .HasDatabaseName("IX_email_log_event_registration");
    }
}
