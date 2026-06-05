using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

public class WaitlistEntry : Entity<WaitlistEntryId>
{
    // Required for EF Core
    // ReSharper disable once UnusedMember.Local
    private WaitlistEntry()
    {
    }

    internal WaitlistEntry(WaitlistEntryId id, EmailAddress email, int position, DateTimeOffset addedAt)
        : base(id)
    {
        Email = email;
        Position = position;
        AddedAt = addedAt;
    }

    public EmailAddress Email { get; private set; }
    public int Position { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
    public WaitlistEntryStatus Status { get; private set; }

    internal void Remove()
    {
        Status = WaitlistEntryStatus.Removed;
    }

    internal void UpdatePosition(int newPosition)
    {
        Position = newPosition;
    }
}
