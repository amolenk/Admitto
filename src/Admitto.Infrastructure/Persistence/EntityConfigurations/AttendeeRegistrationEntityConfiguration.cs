using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class AttendeeRegistrationEntityConfiguration : IEntityTypeConfiguration<AttendeeRegistration>
{
    public void Configure(EntityTypeBuilder<AttendeeRegistration> builder)
    {
        builder.ToTable("attendee_registrations");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        
        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.FirstName)
            .HasColumnName("first_name")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.LastName)
            .HasColumnName("last_name")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.OrganizationName)
            .HasColumnName("organization_name")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .HasConversion(p => p.Value, p => new TeamId(p))
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .HasConversion(p => p.Value, p => new TicketedEventId(p))
            .IsRequired();
        
        builder.OwnsOne(e => e.TicketOrder, b =>
        {
            b.ToJson("ticket_order");
        });
        
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();
    }
}
