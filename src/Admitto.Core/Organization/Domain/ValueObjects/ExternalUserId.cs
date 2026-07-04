using Vogen;

namespace Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier for a user in an external user directory (e.g. Keycloak or Auth0 sub claim).
/// </summary>
[ValueObject<string>]
public partial struct ExternalUserId
{
    private static Validation Validate(string value)
        => !string.IsNullOrWhiteSpace(value) ? Validation.Ok : Validation.Invalid("External user ID cannot be empty.");
}
