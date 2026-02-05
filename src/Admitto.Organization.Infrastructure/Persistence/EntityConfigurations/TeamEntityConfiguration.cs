using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
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
            .HasConversion(v => v.Value, v => new TeamId(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .HasConversion(v => v.Value, v => TeamSlug.From(v))
            .IsRequired()
            .HasMaxLength(Slug.MaxLength);

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasConversion<string>(v => v.Value, v => EmailAddress.From(v))
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);

        builder.OwnsMany(e => e.Members, b =>
        {
            b.ToJson("members");
            
            b.Property(e => e.Id)
                .HasConversion(v => v.Value, v => new UserId(v));
        });
        
        builder.HasIndex(e => e.Slug)
            .IsUnique();
    }
}
