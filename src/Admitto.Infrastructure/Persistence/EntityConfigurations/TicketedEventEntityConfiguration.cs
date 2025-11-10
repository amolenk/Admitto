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

        builder.Property(e => e.StartsAt)
            .HasColumnName("start_time")
            .IsRequired();

        builder.Property(e => e.EndsAt)
            .HasColumnName("end_time")
            .IsRequired();

        builder.Property(e => e.BaseUrl)
            .HasColumnName("base_url")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.Url);
        
        builder.OwnsMany(e => e.AdditionalDetailSchemas, b =>
        {
            b.ToJson("additional_detail_schemas");
        });
        
        builder.OwnsMany(e => e.TicketTypes, b =>
        {
            b.ToJson("ticket_types");
        });
        
        builder.OwnsOne<CancellationPolicy>(e => e.CancellationPolicy, b =>
        {
            b.ToJson("cancellation_policy");
        });

        builder.OwnsOne<ReconfirmPolicy>(e => e.ReconfirmPolicy, b =>
        {
            b.ToJson("reconfirm_policy");
        });

        builder.OwnsOne<RegistrationPolicy>(e => e.RegistrationPolicy, b =>
        {
            b.ToJson("registration_policy");
        });

        builder.OwnsOne<ReminderPolicy>(e => e.ReminderPolicy, b =>
        {
            b.ToJson("reminder_policy");
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
