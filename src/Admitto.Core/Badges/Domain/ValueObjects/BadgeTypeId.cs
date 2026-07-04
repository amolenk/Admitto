using Vogen;

namespace Amolenk.Admitto.Core.Badges.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct BadgeTypeId
{
    public static BadgeTypeId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("BadgeType ID cannot be empty.");
}
