using Amolenk.Admitto.Application.Common.Email.Sending;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// Configuration for the SentEmailLog entity.
/// </summary>
public class SentEmailLogEntityConfiguration : IEntityTypeConfiguration<SentEmailLog>
{
    public void Configure(EntityTypeBuilder<SentEmailLog> builder)
    {
        builder.ToTable("sent_email_log");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.IdempotencyKey)
            .HasColumnName("dispatch_id")
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        
        builder
            .HasIndex(e => new { e.TicketedEventId, DispatchId = e.IdempotencyKey, e.Email })
            .IsUnique();
    }
}
