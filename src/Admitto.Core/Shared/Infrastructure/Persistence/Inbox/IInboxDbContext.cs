using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;

public interface IInboxDbContext
{
    DbSet<ProcessedMessage> ProcessedMessages { get; }

    ChangeTracker ChangeTracker { get; }
}
