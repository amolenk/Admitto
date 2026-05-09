using Vogen;

namespace Amolenk.Admitto.Core.Module.Organization.Domain.ValueObjects;

/// <summary>
/// Surrogate identifier for a <c>TeamEventCreationRequest</c>. Used to correlate
/// the eventual <c>TicketedEventCreated</c> / <c>TicketedEventCreationRejected</c>
/// integration event back to the originating create-event request, independently
/// of the (potentially-not-yet-assigned) <c>TicketedEventId</c> or slug.
/// </summary>
[ValueObject<Guid>]
public partial struct CreationRequestId
{
    public static CreationRequestId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Creation request ID cannot be empty.");
}

