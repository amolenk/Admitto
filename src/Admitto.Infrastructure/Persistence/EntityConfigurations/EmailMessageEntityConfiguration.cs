using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class EmailMessageEntityConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("email_messages");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .HasConversion(p => p.Value, p => new TeamId(p));

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("ticketed_event_id")
            .HasConversion<Guid?>(
                p => p == null ? null : p.Value, 
                p => p == null ? null : new TicketedEventId(p.Value));

        builder.Property(e => e.AttendeeId)
            .HasColumnName("attendee_id")
            .HasConversion<Guid?>(
                p => p == null ? null : p.Value, 
                p => p == null ? null : new AttendeeId(p.Value));

        builder.Property(e => e.RecipientEmail)
            .HasColumnName("recipient_email")
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .IsRequired()
            .HasMaxLength(50); // TODO Large enough?

        builder.Property(e => e.Body)
            .HasColumnName("body")
            .IsRequired()
            .HasMaxLength(2000); // TODO Large enough?
    }
}
