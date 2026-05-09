using Vogen;

namespace Amolenk.Admitto.Core.Module.Organization.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier for a user in the system.
/// </summary>
[ValueObject<Guid>]
public partial struct UserId
{
    public static UserId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("User ID cannot be empty.");
}
