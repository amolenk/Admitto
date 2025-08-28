using Amolenk.Admitto.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// Configuration for the MessageLog entity.
/// </summary>
public class MessageLogEntityConfiguration : IEntityTypeConfiguration<MessageLog>
{
    public void Configure(EntityTypeBuilder<MessageLog> builder)
    {
        builder.ToTable("message_log");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        builder.Property(e => e.MessageType)
            .HasColumnName("message_type")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.HandlerType)
            .HasColumnName("handler_type")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
    }
}
