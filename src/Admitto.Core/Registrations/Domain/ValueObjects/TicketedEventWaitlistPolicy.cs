namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// Event-wide waitlist policy for quiet-hours handling when issuing waitlist offers.
/// </summary>
public sealed record TicketedEventWaitlistPolicy
{
    public static readonly TimeOnly DefaultQuietHoursStart = new(22, 0);
    public static readonly TimeOnly DefaultQuietHoursEnd = new(8, 0);

    public TimeOnly QuietHoursStart { get; private set; } = DefaultQuietHoursStart;
    public TimeOnly QuietHoursEnd { get; private set; } = DefaultQuietHoursEnd;

    // ReSharper disable once UnusedMember.Local — required by EF Core
    private TicketedEventWaitlistPolicy() { }

    private TicketedEventWaitlistPolicy(TimeOnly quietHoursStart, TimeOnly quietHoursEnd)
    {
        QuietHoursStart = quietHoursStart;
        QuietHoursEnd = quietHoursEnd;
    }

    public static TicketedEventWaitlistPolicy Create(TimeOnly quietHoursStart, TimeOnly quietHoursEnd) =>
        new(quietHoursStart, quietHoursEnd);

    public static TicketedEventWaitlistPolicy Default() =>
        Create(DefaultQuietHoursStart, DefaultQuietHoursEnd);
}
