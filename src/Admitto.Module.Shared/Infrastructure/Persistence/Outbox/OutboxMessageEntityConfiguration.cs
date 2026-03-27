using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;

public class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(e => e.Payload)
            .HasColumnName("data")
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.Property(e => e.State)
            .HasColumnName("state")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32);
    }
}
