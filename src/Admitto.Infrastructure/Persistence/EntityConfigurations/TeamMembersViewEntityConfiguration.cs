using Amolenk.Admitto.Application.ReadModel.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class TeamMembersViewEntityConfiguration : IEntityTypeConfiguration<TeamMembersView>
{
    public void Configure(EntityTypeBuilder<TeamMembersView> builder)
    {
        builder.ToTable("team_members");
        builder.HasKey(e => new { e.TeamId, e.UserId});
        
        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(e => e.UserEmail)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Role)
            .HasColumnName("role")
            .IsRequired();
    }
}
