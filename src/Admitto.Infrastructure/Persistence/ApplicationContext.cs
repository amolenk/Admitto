using System.Reflection;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.Common.DTOs;
using Amolenk.Admitto.Application.Common.ReadModels;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class ApplicationContext : DbContext, IApplicationContext
{
    public DbSet<AttendeeActivityReadModel> AttendeeActivities { get; set; } = null!;

    public DbSet<AttendeeRegistration> AttendeeRegistrations { get; set; } = null!;
    
    public DbSet<OutboxMessageDto> Outbox { get; set; } = null!;

    public DbSet<TicketedEvent> TicketedEvents { get; set; } = null!;

    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
        SavingChanges += PublishDomainEventsToOutbox;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
    private void PublishDomainEventsToOutbox(object? sender, SavingChangesEventArgs args)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not AggregateRoot aggregate) continue;
            
            foreach (var domainEvent in aggregate.GetDomainEvents())
            {
                Outbox.Add(OutboxMessageDto.FromDomainEvent(domainEvent));
            }
                
            aggregate.ClearDomainEvents();
        }
    }
}
