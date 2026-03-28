using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class CouponEntityConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => CouponId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .HasConversion(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired();

        builder.Property(e => e.Code)
            .HasColumnName("code")
            .HasConversion(v => v.Value, v => new CouponCode(v))
            .IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasConversion<string>(v => v.Value, v => EmailAddress.From(v))
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

        builder.PrimitiveCollection(e => e.AllowedTicketTypeIds)
            .HasColumnName("allowed_ticket_type_ids")
            .ElementType()
            .HasConversion(new ValueConverter<TicketTypeId, Guid>(
                v => v.Value,
                v => TicketTypeId.From(v)));

        builder.HasIndex(e => e.EventId);
    }
}
