using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        
        builder.Property(e => e.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(e => e.Data)
            .HasColumnName("data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasColumnName("priority")
            .IsRequired();
    }
}
