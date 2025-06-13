using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations.Domain;

public class TeamEntityConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(50);
        
        builder.OwnsOne(e => e.EmailSettings, b =>
        {
            // Initially wanted to store EmailSettings as an owned type with separate columns.
            // However, that doesn't work because this entity already uses OwnsMany with JSON columns. 
            b.ToJson("email_settings");
        });
        
        builder.OwnsMany(e => e.Members, b =>
        {
            b.ToJson("members");
        
            b.Property(m => m.Id).HasColumnName("id");
            b.Property(m => m.Email).HasColumnName("email");
            b.Property(m => m.Role).HasColumnName("role")
                .HasConversion(
                    r => r.Value,
                    v => new TeamMemberRole(v));
        });
        
        builder.OwnsMany(e => e.ActiveEvents, b =>
        {
            b.ToJson("active_events");
            
            // Even though it's all JSON, EF Core still needs to know the structure of the data
            b.OwnsMany(t => t.TicketTypes);
        });
    }
}
