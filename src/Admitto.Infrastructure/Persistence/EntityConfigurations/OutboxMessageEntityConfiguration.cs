using Amolenk.Admitto.Application.Common.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageDto>
{
    public void Configure(EntityTypeBuilder<OutboxMessageDto> builder)
    {
        builder.ToTable("outbox");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        
        builder.Property(e => e.Discriminator)
            .HasColumnName("discriminator")
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
