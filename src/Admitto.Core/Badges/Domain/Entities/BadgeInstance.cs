using Amolenk.Admitto.Core.Shared.Kernel.Entities;

namespace Amolenk.Admitto.Core.Badges.Domain.Entities;

/// <summary>
/// A manually managed badge instance belonging to a standalone <see cref="BadgeType"/>.
/// </summary>
public sealed class BadgeInstance : Aggregate<BadgeInstanceId>
{
    private BadgeInstance() { }

    private BadgeInstance(
        BadgeInstanceId id,
        BadgeTypeId badgeTypeId,
        BadgeInstanceDisplayName displayName,
        BadgeInstanceNotes notes)
        : base(id)
    {
        BadgeTypeId = badgeTypeId;
        DisplayName = displayName;
        Notes = notes;
    }

    public BadgeTypeId BadgeTypeId { get; private set; }
    public BadgeInstanceDisplayName DisplayName { get; private set; }
    public BadgeInstanceNotes Notes { get; private set; }

    public static BadgeInstance Create(
        BadgeInstanceId id,
        BadgeTypeId badgeTypeId,
        BadgeInstanceDisplayName displayName,
        BadgeInstanceNotes notes)
        => new(id, badgeTypeId, displayName, notes);

    public void Update(BadgeInstanceDisplayName displayName, BadgeInstanceNotes notes)
    {
        DisplayName = displayName;
        Notes = notes;
    }
}
