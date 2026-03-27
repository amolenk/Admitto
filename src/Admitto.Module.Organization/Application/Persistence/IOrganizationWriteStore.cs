using Amolenk.Admitto.Module.Organization.Domain.Entities;

namespace Amolenk.Admitto.Module.Organization.Application.Persistence;

public interface IOrganizationWriteStore
{
    DbSet<Team> Teams { get; }

    DbSet<TicketedEvent> TicketedEvents { get; }
    
    DbSet<User> Users { get; }
}