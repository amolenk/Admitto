using Amolenk.Admitto.Application.Projections.TeamMember;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class TeamMemberViewEntityConfiguration : IEntityTypeConfiguration<TeamMemberView>
{
    public void Configure(EntityTypeBuilder<TeamMemberView> builder)
    {
        builder.ToTable("team_members");
        builder.HasKey(e => new { e.UserId, e.TeamId });
        
        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.AssignedAt)
            .HasColumnName("assigned_at")
            .IsRequired();
    }
}
