using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class TeamEmailContextViewEntityConfiguration
    : IEntityTypeConfiguration<TeamEmailContextView>
{
    public void Configure(EntityTypeBuilder<TeamEmailContextView> builder)
    {
        builder.ToTable("team_email_context_view");
        builder.HasKey(e => e.TeamId);

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TeamName)
            .HasColumnName("team_name")
            .HasMaxLength(TeamName.MaxLength);

        builder.Property(e => e.AccentColor)
            .HasColumnName("accent_color")
            .HasMaxLength(EmailAccentColor.MaxLength);

        builder.Property(e => e.TeamVersion)
            .HasColumnName("team_version")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.LastUpdatedAt)
            .HasColumnName("last_updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();
    }
}
