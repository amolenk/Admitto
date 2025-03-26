using Amolenk.Admitto.Application.ReadModel.Views;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class AttendeeActivityViewEntityConfiguration : IEntityTypeConfiguration<AttendeeActivityView>
{
    public void Configure(EntityTypeBuilder<AttendeeActivityView> builder)
    {
        builder.ToTable("attendee_activities");
        
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(e => e.AttendeeId)
            .HasColumnName("attendee_id")
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.Property(e => e.Activity)
            .HasColumnName("activity")
            .IsRequired()
            .HasMaxLength(255);
    }
}
