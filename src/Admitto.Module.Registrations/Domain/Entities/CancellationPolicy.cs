using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Per-event policy describing when an attendee cancellation is considered late.
/// Optional — absence means no cancellation is ever classified as late.
/// </summary>
public class CancellationPolicy : Aggregate<TicketedEventId>
{
    private CancellationPolicy() { }

    private CancellationPolicy(TicketedEventId id, DateTimeOffset lateCancellationCutoff)
        : base(id)
    {
        LateCancellationCutoff = lateCancellationCutoff;
    }

    public DateTimeOffset LateCancellationCutoff { get; private set; }

    public static CancellationPolicy Create(TicketedEventId eventId, DateTimeOffset lateCancellationCutoff)
        => new(eventId, lateCancellationCutoff);

    public void UpdateCutoff(DateTimeOffset lateCancellationCutoff)
    {
        LateCancellationCutoff = lateCancellationCutoff;
    }

    /// <summary>
    /// Classifies a cancellation at the given instant.
    /// Returns true if the cancellation is "late" (at or after the cutoff).
    /// </summary>
    public bool IsLateCancellation(DateTimeOffset cancellationInstant)
        => cancellationInstant >= LateCancellationCutoff;
}
