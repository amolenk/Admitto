using Amolenk.Admitto.Application.Projections.Participation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class ParticipationViewEntityConfiguration : IEntityTypeConfiguration<ParticipationView>
{
    public void Configure(EntityTypeBuilder<ParticipationView> builder)
    {
        builder.ToTable("participation_view");
        
        builder.HasKey(e => new { e.TicketedEventId, e.RegistrationId });

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(ColumnMaxLength.EmailAddress)
            .IsRequired();
        
        builder.Property(e => e.AttendeeStatus)
            .HasColumnName("attendee_status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.ContributorRole)
            .HasColumnName("contributor_role")
            .HasConversion<string>()
            .HasMaxLength(32);
        
        builder.Property(e => e.LastModifiedAt)
            .HasColumnName("last_modified_at")
            .IsRequired();
    }
}
