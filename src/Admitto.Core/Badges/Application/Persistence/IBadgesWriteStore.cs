using Amolenk.Admitto.Core.Badges.Domain.Entities;

namespace Amolenk.Admitto.Core.Badges.Application.Persistence;

public interface IBadgesWriteStore
{
    DbSet<BadgeEvent> BadgeEvents { get; }
    DbSet<BadgeInstance> BadgeInstances { get; }
}
