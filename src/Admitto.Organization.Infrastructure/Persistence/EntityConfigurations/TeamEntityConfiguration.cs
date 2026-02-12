using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Organization.Infrastructure.Persistence.EntityConfigurations;

public class TeamEntityConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(Slug.MaxLength);

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(DisplayName.MaxLength);

        builder.Property(e => e.EmailAddress)
            .HasColumnName("email_address")
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);
        
        builder.HasIndex(e => e.Slug)
            .IsUnique();
    }
}
