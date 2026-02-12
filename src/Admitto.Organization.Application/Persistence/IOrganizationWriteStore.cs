using Amolenk.Admitto.Organization.Domain.Entities;

namespace Amolenk.Admitto.Organization.Application.Persistence;

public interface IOrganizationWriteStore
{
    DbSet<Team> Teams { get; }

    // DbSet<TicketedEvent> TicketedEvents { get; }
    
    DbSet<User> Users { get; }
}