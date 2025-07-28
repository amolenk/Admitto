using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class TeamEntityConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.Slug)
            .IsUnique();
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EmailServiceConnectionString)
            .HasColumnName("email_service")
            .HasColumnType("text") // Use text to allow larger encrypted strings
            .IsRequired();
        
        builder.OwnsMany(e => e.Members, b =>
        {
            b.ToJson("members");
        });
    }
}
