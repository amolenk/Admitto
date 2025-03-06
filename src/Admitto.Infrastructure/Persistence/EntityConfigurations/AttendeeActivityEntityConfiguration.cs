using Amolenk.Admitto.Application.Common.ReadModels;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class AttendeeActivityEntityConfiguration : IEntityTypeConfiguration<AttendeeActivityReadModel>
{
    public void Configure(EntityTypeBuilder<AttendeeActivityReadModel> builder)
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
