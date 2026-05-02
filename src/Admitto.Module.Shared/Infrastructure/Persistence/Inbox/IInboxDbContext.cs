using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Inbox;

public interface IInboxDbContext
{
    DbSet<ProcessedMessage> ProcessedMessages { get; }

    ChangeTracker ChangeTracker { get; }
}
