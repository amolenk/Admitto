using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class TicketTypeEntityConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("ticket_types");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        
        builder.Property(e => e.MaxCapacity)
            .HasColumnName("max_capacity")
            .IsRequired();

        builder.Property(e => e.RemainingCapacity)
            .HasColumnName("remaining_capacity")
            .IsRequired();

        builder.Property(e => e.SessionName)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.SessionStartDateTime)
            .HasColumnName("start_date_time")
            .IsRequired();
        
        builder.Property(e => e.SessionEndDateTime)
            .HasColumnName("end_date_time")
            .IsRequired();
    }
}
