using Amolenk.Admitto.Application.Projections.ParticipantActivity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class ParticipantActivityViewEntityConfiguration : IEntityTypeConfiguration<ParticipantActivityView>
{
    public void Configure(EntityTypeBuilder<ParticipantActivityView> builder)
    {
        builder.ToTable("vw_participant_activities");
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

        builder.Property(e => e.SourceId)
            .HasColumnName("source_id")
            .IsRequired();

        builder.Property(e => e.Activity)
            .HasColumnName("activity")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.EmailLogId)
            .HasColumnName("email_log_id");

        builder.Property(e => e.OccuredOn)
            .HasColumnName("occured_on")
            .IsRequired();
        
        builder
            .HasIndex(e => new { e.TicketedEventId, e.ParticipantId, e.SourceId, e.Activity })
            .IsUnique();
    }
}
