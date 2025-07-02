using System.Reflection;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.ReadModel.Views;
using Amolenk.Admitto.Application.UseCases.Email;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class ApplicationContext(DbContextOptions options) : DbContext(options), IDomainContext, IReadModelContext,
    IEmailContext, IJobContext
{
    // IDomainContext sets
    public DbSet<AttendeeRegistration> AttendeeRegistrations { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<TicketedEvent> TicketedEvents { get; set; } = null!;
    
    // IReadModelContext sets
    public DbSet<AttendeeActivityView> AttendeeActivities { get; set; } = null!;
    
    // IEmailContext sets
    public DbSet<EmailMessage> EmailMessages { get; set; } = null!;
    
    // IJobContext sets
    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<ScheduledJob> ScheduledJobs { get; set; } = null!;
    
    // Other sets
    public DbSet<OutboxMessage> Outbox { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
