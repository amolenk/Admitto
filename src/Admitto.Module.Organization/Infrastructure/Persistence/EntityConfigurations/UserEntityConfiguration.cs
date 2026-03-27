using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.EntityConfigurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.ExternalUserId)
            .HasColumnName("external_user_id");

        builder.Property(e => e.EmailAddress)
            .HasColumnName("email_address")
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);
        
        // Can't use ComplexTypes yet because read-only collections aren't supported, see
        // https://github.com/dotnet/efcore/issues/37405
        builder.OwnsMany(
            m => m.Memberships,
            c =>
            {
                c.ToJson("memberships");
        
                c.Property(m => m.Id)
                    .HasJsonPropertyName("id");

                c.Property(m => m.Role)
                    .HasConversion<string>()
                    .HasJsonPropertyName("role");
            });
        
        builder.HasIndex(e => e.EmailAddress)
            .IsUnique();
    }
}
