using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Application.Common.Identity;
using Amolenk.Admitto.Application.Projections.ParticipantHistory;
using Amolenk.Admitto.Application.Projections.Participation;
using Amolenk.Admitto.Domain.Contracts;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class ApplicationContext(DbContextOptions options, IDataProtectionProvider dataProtectionProvider)
    : DbContext(options), IApplicationContext
{
    public DbSet<BulkEmailWorkItem> BulkEmailJobs { get; set; } = null!;
    public DbSet<Attendee> Attendees { get; set; } = null!;
    public DbSet<Contributor> Contributors { get; set; } = null!;
    public DbSet<EmailLog> EmailLog { get; set; } = null!;
    public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;
    public DbSet<EmailVerificationRequest> EmailVerificationRequests { get; set; } = null!;
    public DbSet<Message> Outbox { get; set; } = null!;
    public DbSet<MessageLog> MessageLogs { get; set; } = null!;
    public DbSet<Participant> Participants { get; set; } = null!;
    public DbSet<ParticipantHistoryView> AttendeeActivityView { get; set; } = null!;
    public DbSet<ParticipationView> AdmissionView { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<TicketedEvent> TicketedEvents { get; set; } = null!;
    public DbSet<TicketedEventAvailability> TicketedEventAvailability { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply entity configurations using explicit instances.
        // We don't use modelBuilder.ApplyConfigurationsFromAssembly here because some configurations
        // require DI parameters (e.g. IDataProtectionProvider).
        modelBuilder.ApplyConfiguration(new AttendeeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BulkEmailWorkItemEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContributorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmailLogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmailTemplateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmailVerificationRequestEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MessageLogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipantEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipantHistoryViewEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipationViewEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TeamEntityConfiguration(dataProtectionProvider));
        modelBuilder.ApplyConfiguration(new TicketedEventAvailabilityEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TicketedEventEntityConfiguration(dataProtectionProvider));
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (typeof(IHasConcurrencyToken).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IHasConcurrencyToken.Version))
                    .IsRowVersion();
            }
            
            if (typeof(IIsAuditable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IIsAuditable.CreatedAt))
                    .HasColumnName("created_at")
                    .IsRequired();
            
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IIsAuditable.LastChangedAt))
                    .HasColumnName("last_changed_at")
                    .IsRequired();
            
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IIsAuditable.LastChangedBy))
                    .HasColumnName("last_changed_by")
                    .HasMaxLength(50);
            }
        }
    }
}
