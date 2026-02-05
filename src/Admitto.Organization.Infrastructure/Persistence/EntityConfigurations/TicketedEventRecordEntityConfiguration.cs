using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Organization.Infrastructure.Persistence.EntityConfigurations;

public class TicketedEventRecordEntityConfiguration : IEntityTypeConfiguration<TicketedEventRecord>
{
    public void Configure(EntityTypeBuilder<TicketedEventRecord> builder)
    {
        builder.ToTable("ticketed_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(Slug.MaxLength);

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Website)
            .HasColumnName("website_url")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.BaseUrl)
            .HasColumnName("callback_base_url")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.StartsAt)
            .HasColumnName("starts_at")
            .IsRequired();

        builder.Property(e => e.EndsAt)
            .HasColumnName("ends_at")
            .IsRequired();

        builder.ComplexCollection(
            e => e.TicketTypes,
            b =>
            {
                b.ToJson("ticket_types");
            });
        
        builder.HasIndex(e => e.Slug)
            .IsUnique();
    }
}