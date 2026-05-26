using Vogen;

namespace Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

[ValueObject<Guid>]
public partial struct TicketTypeId
{
    public static TicketTypeId New() => From(Guid.NewGuid());

    public static List<TicketTypeId> ListFrom(IEnumerable<Guid> ids)
    {
        return ids.Select(From).ToList();
    }

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("TicketType ID cannot be empty.");
}

