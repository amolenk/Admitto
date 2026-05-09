using Vogen;

namespace Amolenk.Admitto.Core.Module.Organization.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct ApiKeyId
{
    public static ApiKeyId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("API key ID cannot be empty.");
}

