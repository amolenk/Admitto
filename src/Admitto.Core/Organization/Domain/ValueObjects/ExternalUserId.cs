using Vogen;

namespace Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier for a user in an external user directory.
/// </summary>
[ValueObject<Guid>]
public partial struct ExternalUserId
{
    public static ExternalUserId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("External user ID cannot be empty.");
}
