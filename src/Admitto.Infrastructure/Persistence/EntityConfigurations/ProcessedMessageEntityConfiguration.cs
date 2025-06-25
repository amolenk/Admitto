using Amolenk.Admitto.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class ProcessedMessageEntityConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");
        
        builder.HasKey(e => e.MessageId);
        
        builder.Property(e => e.MessageId)
            .HasColumnName("message_id")
            .ValueGeneratedNever();
        
        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
        
        // Create an index on ProcessedAt for potential cleanup operations
        builder.HasIndex(e => e.ProcessedAt)
            .HasDatabaseName("ix_processed_messages_processed_at");
    }
}