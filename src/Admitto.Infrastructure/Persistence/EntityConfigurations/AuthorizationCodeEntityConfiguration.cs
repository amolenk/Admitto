using Amolenk.Admitto.Application.UseCases.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class AuthorizationCodeEntityConfiguration : IEntityTypeConfiguration<AuthorizationCode>
{
    public void Configure(EntityTypeBuilder<AuthorizationCode> builder)
    {
        builder.ToTable("authorization_codes");
        builder.HasKey(e => e.Code);
        
        builder.Property(e => e.Code)
            .HasColumnName("code")
            .ValueGeneratedNever();
        
        builder.Property(e => e.CodeChallenge)
            .HasColumnName("code_challenge")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Expires)
            .HasColumnName("expires")
            .IsRequired();
    }
}
