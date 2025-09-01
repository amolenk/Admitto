using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class TeamEntityConfiguration(IDataProtectionProvider dataProtectionProvider) : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        var protector = dataProtectionProvider.CreateProtector("Admitto");
        
        builder.ToTable("teams");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.HasIndex(e => e.Slug)
            .IsUnique();
        
        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.Slug);

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.TeamName);

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.EmailAddress);

        builder.Property(e => e.EmailServiceConnectionString)
            .HasColumnName("email_service")
            .HasColumnType("text") // Use text to allow larger encrypted strings
            .HasConversion(new SecretProtectorConverter(protector))
            .IsRequired();
        
        builder.OwnsMany(e => e.Members, b =>
        {
            b.ToJson("members");
        });
    }
}
