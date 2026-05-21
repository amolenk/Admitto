using Amolenk.Admitto.Core.Badges.Domain.Entities;

namespace Amolenk.Admitto.Core.Badges.Application.Persistence;

public interface IBadgesWriteStore
{
    DbSet<BadgesEvent> BadgesEvents { get; }
    DbSet<BadgeType> BadgeTypes { get; }
    DbSet<BadgeInstance> BadgeInstances { get; }
}
