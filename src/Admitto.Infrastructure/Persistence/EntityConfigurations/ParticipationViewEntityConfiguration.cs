using Amolenk.Admitto.Application.Projections.Participation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class ParticipationViewEntityConfiguration : IEntityTypeConfiguration<ParticipationView>
{
    public void Configure(EntityTypeBuilder<ParticipationView> builder)
    {
        builder.ToTable("vw_participation");
        builder.HasKey(e => e.ParticipantId);
        
        builder.Property(e => e.ParticipantId)
            .HasColumnName("participant_id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.PublicId)
            .HasColumnName("public_id")
            .IsRequired();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(ColumnMaxLength.EmailAddress)
            .IsRequired();

        builder.Property(e => e.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(ColumnMaxLength.FirstName)
            .IsRequired();

        builder.Property(e => e.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(ColumnMaxLength.LastName)
            .IsRequired();

        builder.Property(e => e.AttendeeStatus)
            .HasColumnName("attendee_status")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(e => e.AttendeeId)
            .HasColumnName("attendee_id");

        builder.Property(e => e.ContributorStatus)
            .HasColumnName("contributor_status")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(e => e.ContributorId)
            .HasColumnName("contributor_id");

        builder.Property(e => e.LastModifiedAt)
            .HasColumnName("last_modified_at")
            .IsRequired();

        builder.HasIndex(e => new { e.TicketedEventId, e.PublicId })
            .IsUnique();
    }
}
