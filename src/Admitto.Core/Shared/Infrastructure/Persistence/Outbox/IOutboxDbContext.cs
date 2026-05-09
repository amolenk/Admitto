using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
    
    ChangeTracker ChangeTracker { get; }
}