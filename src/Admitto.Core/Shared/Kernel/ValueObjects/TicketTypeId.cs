using Vogen;

namespace Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

[ValueObject<Guid>]
public partial struct TicketTypeId
{
    public static TicketTypeId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("TicketType ID cannot be empty.");
}

