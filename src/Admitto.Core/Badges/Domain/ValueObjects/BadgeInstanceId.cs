using Vogen;

namespace Amolenk.Admitto.Core.Badges.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct BadgeInstanceId
{
    public static BadgeInstanceId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("BadgeInstance ID cannot be empty.");
}
