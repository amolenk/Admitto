using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Inbox;

public class ProcessedMessageEntityConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.MessageKey)
            .HasColumnName("message_key")
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();

        builder.HasIndex(e => e.MessageKey)
            .IsUnique()
            .HasDatabaseName("ix_processed_messages_message_key");
    }
}
