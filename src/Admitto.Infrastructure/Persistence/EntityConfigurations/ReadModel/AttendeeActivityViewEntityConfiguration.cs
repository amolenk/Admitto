using Amolenk.Admitto.Application.ReadModel.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations.ReadModel;

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

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Activity)
            .HasColumnName("activity")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.HasIndex(e => e.Email);
    }
}
