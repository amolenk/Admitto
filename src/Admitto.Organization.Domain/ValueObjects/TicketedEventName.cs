using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public readonly record struct TicketedEventName : IStringValueObject
{
    private const int MaxLength = 100;

    public string Value { get; }

    private TicketedEventName(string value) => Value = value;

    public static ValidationResult<TicketedEventName> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MaxLength,
            v => new TicketedEventName(v));

    public static TicketedEventName From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value;
}