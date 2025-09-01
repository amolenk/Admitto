using Amolenk.Admitto.Application.Common.Email.Sending;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// Configuration for the EmailLog entity.
/// </summary>
public class EmailLogEntityConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_log");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .IsRequired();

        builder.Property(e => e.Recipient)
            .HasColumnName("recipient")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.EmailAddress);

        builder.Property(e => e.RecipientType)
            .HasColumnName("recipient_type")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.EmailType)
            .HasColumnName("email_type")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.EmailType);

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.EmailSubject);

        builder.Property(e => e.Provider)
            .HasColumnName("provider")
            .IsRequired()
            .HasMaxLength(32);
        
        builder.Property(e => e.ProviderMessageId)
            .HasColumnName("provider_message_id")
            .HasMaxLength(255);
        
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.EmailStatus);
        
        builder.Property(e => e.SentAt)
            .HasColumnName("sent_at")
            .IsRequired();

        builder.Property(e => e.StatusUpdatedAt)
            .HasColumnName("status_updated_at")
            .IsRequired();

        builder.Property(e => e.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(255);
        
        builder
            .HasIndex(e => new { e.TicketedEventId, e.IdempotencyKey, e.Recipient })
            .IsUnique();
    }
}
