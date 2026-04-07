using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.EntityConfigurations;

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

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(EventStatus.Active);

        builder.HasIndex(e => new { e.TeamId, e.Slug })
            .IsUnique();
    }
}
