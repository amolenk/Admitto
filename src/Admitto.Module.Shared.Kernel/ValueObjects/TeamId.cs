using Vogen;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

[ValueObject<Guid>]
public partial struct TeamId
{
    public static TeamId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Team ID cannot be empty.");
}
