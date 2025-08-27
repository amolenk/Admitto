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
            .HasIndex(e => new { e.TicketedEventId, e.IdempotencyKey, e.Email })
            .IsUnique();
    }
}
