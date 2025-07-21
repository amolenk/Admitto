using System.Collections.ObjectModel;
using System.Reflection;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.ReadModel.Views;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class ApplicationContext(DbContextOptions options) : DbContext(options), IDomainContext, IReadModelContext
{
    // IDomainContext sets
    // public DbSet<Registration> Registrations { get; set; } = null!;
    public DbSet<Attendee> Attendees { get; set; } = null!;
    public DbSet<CrewMember> CrewMembers { get; set; } = null!;
    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;
    public DbSet<ScheduledJob> ScheduledJobs { get; set; } = null!;
    public DbSet<Speaker> Speakers { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<TicketedEvent> TicketedEvents { get; set; } = null!;

    // IReadModelContext sets
    public DbSet<AttendeeActivityView> AttendeeActivities { get; set; } = null!;
    
    // Other sets
    public DbSet<Message> Outbox { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (typeof(IHasAdditionalDetails).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .OwnsMany(typeof(AdditionalDetail), nameof(IHasAdditionalDetails.AdditionalDetails), b =>
                    {
                        b.ToJson("additional_details");
                    });
            }
            
            if (typeof(IHasConcurrencyToken).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IHasConcurrencyToken.Version))
                    .IsRowVersion();
            }
            
            if (typeof(IAuditable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IAuditable.CreatedAt))
                    .HasColumnName("created_at")
                    .IsRequired();
            
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IAuditable.LastChangedAt))
                    .HasColumnName("last_changed_at")
                    .IsRequired();
            
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IAuditable.LastChangedBy))
                    .HasColumnName("last_changed_by")
                    .HasMaxLength(50);
            }
        }
    }
}
