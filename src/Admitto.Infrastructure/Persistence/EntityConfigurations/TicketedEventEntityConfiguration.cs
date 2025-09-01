using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class TicketedEventEntityConfiguration(IDataProtectionProvider dataProtectionProvider)
    : IEntityTypeConfiguration<TicketedEvent>
{
    public void Configure(EntityTypeBuilder<TicketedEvent> builder)
    {
        var protector = dataProtectionProvider.CreateProtector("Admitto");
        
        builder.ToTable("ticketed_events");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.Slug);

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.TicketedEventName);

        builder.Property(e => e.Website)
            .HasColumnName("website")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.Url);

        builder.Property(e => e.StartTime)
            .HasColumnName("start_time")
            .IsRequired();

        builder.Property(e => e.EndTime)
            .HasColumnName("end_time")
            .IsRequired();

        builder.Property(e => e.BaseUrl)
            .HasColumnName("base_url")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.Url);
        
        builder.OwnsOne<TicketedEventPolicies>(e => e.ConfiguredPolicies, b =>
            {
                b.ToJson("policies");

                b.OwnsOne<CancellationPolicy>(p => p.CancellationPolicy, cpb =>
                {
                    cpb.ToJson();
                });
                
                b.OwnsOne<RegistrationPolicy>(p => p.RegistrationPolicy, rpb =>
                {
                    rpb.ToJson();
                });

            });

        builder.Property(e => e.SigningKey)
            .HasColumnName("signing_key")
            .HasColumnType("text") // Use text to allow larger encrypted strings
            .HasConversion(new SecretProtectorConverter(protector))
            .IsRequired();

        builder
            .HasIndex(e => new { e.TeamId, e.Slug })
            .IsUnique();
    }
}
