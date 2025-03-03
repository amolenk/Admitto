using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<TicketedEvent> TicketedEvents { get; }
    
    DbSet<OutboxMessage> Outbox { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}