namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

/// <summary>
/// Value-object policy describing an optional late-cancellation cutoff.
/// Absent cutoff means no cancellation is ever classified as "late".
/// </summary>
public sealed record TicketedEventCancellationPolicy(DateTimeOffset LateCancellationCutoff)
{
    public bool IsLateCancellation(DateTimeOffset cancellationInstant) =>
        cancellationInstant >= LateCancellationCutoff;
}
