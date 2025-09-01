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
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.ParticipantId)
            .HasColumnName("participant_id")
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.EmailAddress);

        builder.Property(e => e.FirstName)
            .HasColumnName("first_name")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.FirstName);

        builder.Property(e => e.LastName)
            .HasColumnName("last_name")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.LastName);
    
        builder.OwnsMany(e => e.AdditionalDetails, b =>
        {
            b.ToJson("additional_details");
        });
        
        builder.OwnsMany(e => e.Tickets, b =>
        {
            b.ToJson("tickets");
        });

        builder.Property(e => e.RegistrationStatus)
            .HasConversion<string>()
            .HasColumnName("status")
            .IsRequired();
        
        builder.HasIndex(e => e.Email).IsUnique();
    }
}
