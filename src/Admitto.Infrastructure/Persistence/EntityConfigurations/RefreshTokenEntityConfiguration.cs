using Amolenk.Admitto.Application.UseCases.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class RefreshTokensEntityConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(e => e.Token);
        
        builder.Property(e => e.Token)
            .HasColumnName("token")
            .ValueGeneratedNever();
    }
}
