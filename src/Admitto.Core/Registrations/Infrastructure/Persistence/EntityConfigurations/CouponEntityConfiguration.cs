using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class CouponEntityConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Code)
            .HasColumnName("code")
            .IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);

        builder.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(e => e.BypassRegistrationWindow)
            .HasColumnName("bypass_registration_window")
            .IsRequired();

        builder.Property(e => e.RedeemedAt)
            .HasColumnName("redeemed_at");

        builder.Property(e => e.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(e => e.Source)
            .HasColumnName("source")
            .IsRequired()
            .HasConversion<int>();

        builder.PrimitiveCollection(e => e.AllowedTicketTypeIds)
            .HasColumnName("allowed_ticket_type_ids")
            .IsRequired()
            .ElementType(et => et.HasConversion<TicketTypeId.EfCoreValueConverter>());

        builder.HasIndex(e => new { e.EventId, e.TeamId })
            .HasDatabaseName("IX_coupons_event_id_team_id");
    }
}
