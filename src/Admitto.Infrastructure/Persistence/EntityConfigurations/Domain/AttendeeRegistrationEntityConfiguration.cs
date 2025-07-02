using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations.Domain;

public class AttendeeRegistrationEntityConfiguration : IEntityTypeConfiguration<AttendeeRegistration>
{
    public void Configure(EntityTypeBuilder<AttendeeRegistration> builder)
    {
        builder.ToTable("attendee_registrations");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .HasConversion(p => p.Value, p => new TicketedEventId(p))
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.FirstName)
            .HasColumnName("first_name")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.LastName)
            .HasColumnName("last_name")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.OwnsMany(e => e.Details, b =>
        {
            b.ToJson("details");
        });

        builder.OwnsMany(e => e.Tickets, b =>
        {
            b.ToJson("tickets");
            
            b.Property(m => m.TicketTypeId)
                .HasConversion(
                    r => r.Value,
                    v => new TicketTypeId(v));
        });
    }
}
