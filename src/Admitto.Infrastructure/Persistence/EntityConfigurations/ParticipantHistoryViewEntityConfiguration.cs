using Amolenk.Admitto.Application.Projections.ParticipantHistory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class ParticipantHistoryViewEntityConfiguration : IEntityTypeConfiguration<ParticipantHistoryView>
{
    public void Configure(EntityTypeBuilder<ParticipantHistoryView> builder)
    {
        builder.ToTable("vw_participant_history");
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
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.EmailType)
            .HasColumnName("email_type")
            .HasMaxLength(ColumnMaxLength.EmailType)
            .IsRequired(false);

        builder.Property(e => e.EmailLogId)
            .HasColumnName("email_log_id");

        builder.Property(e => e.OccuredAt)
            .HasColumnName("occured_at")
            .IsRequired();
        
        builder
            .HasIndex(e => new { e.TicketedEventId, e.ParticipantId, e.SourceId })
            .IsUnique();
    }
}
