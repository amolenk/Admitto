using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public readonly record struct TicketTypeAdminLabel : IStringValueObject
{
    private const int MaxLength = 100;

    public string Value { get; }

    private TicketTypeAdminLabel(string value) => Value = value;

    public static ValidationResult<TicketTypeAdminLabel> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MaxLength,
            v => new TicketTypeAdminLabel(v));

    public static TicketTypeAdminLabel From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value;
}