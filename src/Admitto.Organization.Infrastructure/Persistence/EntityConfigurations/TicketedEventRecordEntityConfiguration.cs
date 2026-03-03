using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Organization.Infrastructure.Persistence.EntityConfigurations;

public class TicketedEventEntityConfiguration : IEntityTypeConfiguration<TicketedEvent>
{
    public void Configure(EntityTypeBuilder<TicketedEvent> builder)
    {
        builder.ToTable("ticketed_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(DisplayName.MaxLength)
            .IsRequired();

        builder.Property(e => e.WebsiteUrl)
            .HasColumnName("website_url")
            .HasConversion(v => v.Value.ToString(), v => AbsoluteUrl.From(v))
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(e => e.BaseUrl)
            .HasColumnName("base_url")
            .HasConversion(v => v.Value.ToString(), v => AbsoluteUrl.From(v))
            .HasMaxLength(320)
            .IsRequired();

        builder.ComplexProperty(
            e => e.EventWindow,
            b =>
            {
                b.Property(e => e.Start)
                    .HasColumnName("starts_at")
                    .IsRequired();

                b.Property(e => e.End)
                    .HasColumnName("ends_at")
                    .IsRequired();
            });

        // Can't use ComplexTypes yet because read-only collections aren't supported, see
        // https://github.com/dotnet/efcore/issues/37405
        builder.OwnsMany(
            e => e.TicketTypes,
            b =>
            {
                b.ToJson("ticket_types");

                b.Property(tt => tt.Slug)
                    .HasJsonPropertyName("slug");

                b.Property(tt => tt.Name)
                    .HasJsonPropertyName("name");

                b.Property(tt => tt.IsSelfService)
                    .HasJsonPropertyName("isSelfService");

                b.Property(tt => tt.IsSelfServiceAvailable)
                    .HasJsonPropertyName("isSelfServiceAvailable");

                b.Property(tt => tt.TimeSlots)
                    .HasConversion(
                        v => v.Select(ts => ts.Slug.Value).ToArray(),
                        v => v.Select(ts => new TimeSlot(Slug.From(ts))).ToArray())
                    .HasJsonPropertyName("timeSlots");

                b.Property(tt => tt.Capacity)
                    .HasConversion(
                        v => v.HasValue ? v.Value.Value : (int?)null,
                        v => v.HasValue ? Capacity.From(v.Value) : null)
                    .HasJsonPropertyName("capacity");
            });

        builder.HasIndex(e => new { e.TeamId, e.Slug })
            .IsUnique();
    }
}
