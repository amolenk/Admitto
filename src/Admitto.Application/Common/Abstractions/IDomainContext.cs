using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IDomainContext
{
    DbSet<AttendeeRegistration> AttendeeRegistrations { get; }

    DbSet<TicketedEvent> TicketedEvents { get; }
    
    DbSet<Team> Teams { get; }
}