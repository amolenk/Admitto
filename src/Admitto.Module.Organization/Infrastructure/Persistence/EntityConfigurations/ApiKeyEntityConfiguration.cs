using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.EntityConfigurations;

public class ApiKeyEntityConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.KeyPrefix)
            .HasColumnName("key_prefix")
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(e => e.KeyHash)
            .HasColumnName("key_hash")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.RevokedAt)
            .HasColumnName("revoked_at");

        builder.HasIndex(e => e.KeyHash)
            .IsUnique();

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(e => e.TeamId);
    }
}
