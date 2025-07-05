using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IDomainContext
{
    DbSet<AttendeeRegistration> AttendeeRegistrations { get; }
    
    DbSet<Job> Jobs { get; }
    
    DbSet<ScheduledJob> ScheduledJobs { get; }
    
    DbSet<Team> Teams { get; }

    DbSet<TicketedEvent> TicketedEvents { get; }
}