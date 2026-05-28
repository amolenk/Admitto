using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence.EntityConfigurations;

public class BadgesEventEntityConfiguration : IEntityTypeConfiguration<BadgesEvent>
{
    public void Configure(EntityTypeBuilder<BadgesEvent> builder)
    {
        builder.ToTable("badges_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("event_id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();
    }
}
