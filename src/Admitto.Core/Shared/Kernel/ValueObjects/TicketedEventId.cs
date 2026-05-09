using Vogen;

namespace Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

[ValueObject<Guid>]
public partial struct TicketedEventId
{
    public static TicketedEventId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("TicketedEvent ID cannot be empty.");
}

