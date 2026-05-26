using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class WaitlistEntityConfiguration : IEntityTypeConfiguration<Waitlist>
{
    public void Configure(EntityTypeBuilder<Waitlist> builder)
    {
        builder.ToTable("waitlists");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("ticket_type_id")
            .IsRequired()
            .ValueGeneratedNever()
            .HasConversion<TicketTypeId.EfCoreValueConverter>();

        builder.Property(e => e.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.HasIndex(e => e.EventId);

        builder.OwnsMany(e => e.Entries, b =>
        {
            b.ToJson("entries");

            b.Property(e => e.Id)
                .HasJsonPropertyName("id")
                .HasConversion<WaitlistEntryId.EfCoreValueConverter>()
                .IsRequired();

            b.Property(e => e.Email)
                .HasJsonPropertyName("email")
                .IsRequired();

            b.Property(e => e.Position)
                .HasJsonPropertyName("position")
                .IsRequired();

            b.Property(e => e.AddedAt)
                .HasJsonPropertyName("added_at")
                .IsRequired();

            b.Property(e => e.Status)
                .HasJsonPropertyName("status")
                .HasConversion<int>()
                .IsRequired();
        });

        builder.OwnsMany(e => e.Coupons, b =>
        {
            b.ToJson("waitlist_coupons");

            b.Property(e => e.Id)
                .HasJsonPropertyName("id")
                .IsRequired();

            b.Property(e => e.Status)
                .HasJsonPropertyName("status")
                .HasConversion<int>()
                .IsRequired();

            b.Property(e => e.IssuedAt)
                .HasJsonPropertyName("issued_at")
                .IsRequired();
        });
    }
}
