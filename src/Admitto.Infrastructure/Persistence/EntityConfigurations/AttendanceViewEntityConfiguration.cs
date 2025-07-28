using Amolenk.Admitto.Application.Projections.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class AttendanceViewEntityConfiguration : IEntityTypeConfiguration<AttendanceView>
{
    public void Configure(EntityTypeBuilder<AttendanceView> builder)
    {
        builder.ToTable("attendance_view");
        
        builder.HasKey(e => new { e.TeamId, e.TicketedEventId, e.AttendeeId });

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.AttendeeId)
            .HasColumnName("attendee_id")
            .IsRequired();
        
        builder.Property(e => e.AttendanceType)
            .HasConversion<string>()
            .HasColumnName("attendance_type")
            .IsRequired();

        builder.Property(e => e.AttendeeVersion)
            .HasColumnName("attendee_version")
            .IsRequired();
    }
}
