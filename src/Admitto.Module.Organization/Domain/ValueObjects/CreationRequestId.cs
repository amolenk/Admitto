using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.ValueObjects;

/// <summary>
/// Surrogate identifier for a <c>TeamEventCreationRequest</c>. Used to correlate
/// the eventual <c>TicketedEventCreated</c> / <c>TicketedEventCreationRejected</c>
/// integration event back to the originating create-event request, independently
/// of the (potentially-not-yet-assigned) <c>TicketedEventId</c> or slug.
/// </summary>
public readonly record struct CreationRequestId : IGuidValueObject
{
    public Guid Value { get; }

    private CreationRequestId(Guid value) => Value = value;

    public static CreationRequestId New() => new(Guid.NewGuid());

    public static ValidationResult<CreationRequestId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new CreationRequestId(v));

    public static CreationRequestId From(Guid value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
