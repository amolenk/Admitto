using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public readonly record struct TicketTypePublicTitle : IStringValueObject
{
    private const int MaxLength = 100;

    public string Value { get; }

    private TicketTypePublicTitle(string value) => Value = value;

    public static ValidationResult<TicketTypePublicTitle> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MaxLength,
            v => new TicketTypePublicTitle(v));

    public static TicketTypePublicTitle From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value;
}