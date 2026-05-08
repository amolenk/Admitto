using Vogen;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct ActivityLogId
{
    public static ActivityLogId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Activity log ID cannot be empty.");
}

