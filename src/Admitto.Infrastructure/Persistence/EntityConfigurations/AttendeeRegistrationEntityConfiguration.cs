using Amolenk.Admitto.Domain.Entities;
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

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
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
    
        builder.OwnsMany(e => e.Tickets, b =>
        {
            b.ToJson("tickets");
        });

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .IsRequired();
    }
}
