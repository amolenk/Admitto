using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence.Outbox;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
    
    ChangeTracker ChangeTracker { get; }
}