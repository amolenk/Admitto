using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class AttendeeEntityConfiguration : IEntityTypeConfiguration<Attendee>
{
    public void Configure(EntityTypeBuilder<Attendee> builder)
    {
        builder.ToTable("attendees");
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

        builder.OwnsOne(e => e.EmailVerification, b =>
        {
            b.Property(x => x.Code)
                .HasColumnName("email_verification_code")
                .HasMaxLength(6)
                .IsFixedLength()
                .IsRequired();

            b.Property(x => x.ExpirationTime)
                .HasColumnName("email_verification_expiration")
                .IsRequired();
        });
        
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

        builder.Property(e => e.Participation)
            .HasConversion<string>()
            .HasColumnName("participation");

        builder.Property(e => e.ReconfirmedAt)
            .HasColumnName("reconfirmed_at");

        builder.Property(e => e.CheckedInAt)
            .HasColumnName("checked_in_at");

        builder.Property(e => e.CanceledAt)
            .HasColumnName("canceled_at");
    }
}
