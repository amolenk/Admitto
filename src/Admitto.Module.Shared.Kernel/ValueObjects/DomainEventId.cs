using Vogen;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

[ValueObject<Guid>]
public partial struct DomainEventId
{
    public static DomainEventId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("DomainEvent ID cannot be empty.");
}
