using Amolenk.Admitto.Application.Projections.Participation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class ParticipationViewEntityConfiguration : IEntityTypeConfiguration<ParticipationView>
{
    public void Configure(EntityTypeBuilder<ParticipationView> builder)
    {
        builder.ToTable("participation_view");
        
        builder.HasKey(e => new { e.TicketedEventId, e.Email });

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.AttendeeRegistrationId)
            .HasColumnName("attendee_registration_id");

        builder.Property(e => e.AttendeeRegistrationStatus)
            .HasColumnName("attendee_registration_status")
            .HasConversion<string>();

        builder.Property(e => e.AttendeeRegistrationVersion)
            .HasColumnName("attendee_registration_version");
        
        builder.Property(e => e.SpeakerEngagementId)
            .HasColumnName("speaker_engagement_id");

        builder.Property(e => e.SpeakerEngagementVersion)
            .HasColumnName("speaker_engagement_version");
        
        builder.Property(e => e.CrewAssignmentId)
            .HasColumnName("crew_assignment_id");

        builder.Property(e => e.CrewAssignmentVersion)
            .HasColumnName("crew_assignment_version");
        
        builder.Property(e => e.LastModifiedAt)
            .HasColumnName("last_modified_at")
            .IsRequired();
    }
}
