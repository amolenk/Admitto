using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class TicketedEventAvailabilityEntityConfiguration : IEntityTypeConfiguration<TicketedEventAvailability>
{
    public void Configure(EntityTypeBuilder<TicketedEventAvailability> builder)
    {
        builder.ToTable("ticketed_event_availability");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.EmailDomainName)
            .HasColumnName("email_domain_name")
            .HasMaxLength(100);

        builder.Property(e => e.RegistrationStartTime)
            .HasColumnName("registration_start_time")
            .IsRequired();

        builder.Property(e => e.RegistrationEndTime)
            .HasColumnName("registration_end_time")
            .IsRequired();
        
        builder.OwnsMany(e => e.TicketTypes, b =>
        {
            b.ToJson("ticket_types");
        });
        
        builder
            .HasIndex(e => new { e.TicketedEventId })
            .IsUnique();
    }
}
