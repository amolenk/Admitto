using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
    
    ChangeTracker ChangeTracker { get; }
}