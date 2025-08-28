using Amolenk.Admitto.Application.Projections.ParticipantActivity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class ParticipantActivityViewEntityConfiguration : IEntityTypeConfiguration<ParticipantActivityView>
{
    public void Configure(EntityTypeBuilder<ParticipantActivityView> builder)
    {
        builder.ToTable("participant_activity_view");
        
        builder.HasKey(e => new { e.TicketedEventId, e.Email, e.SourceId });

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(ColumnMaxLength.EmailAddress)
            .IsRequired();

        builder.Property(e => e.SourceId)
            .HasColumnName("source_id")
            .IsRequired();

        builder.Property(e => e.Activity)
            .HasColumnName("activity")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.EmailLogId)
            .HasColumnName("email_log_id");

        builder.Property(e => e.OccuredAt)
            .HasColumnName("occured_at")
            .IsRequired();
        
        builder
            .HasIndex(e => new { e.TicketedEventId, e.Email, e.OccuredAt });
    }
}
