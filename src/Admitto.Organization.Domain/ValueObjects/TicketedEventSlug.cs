using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public record TicketedEventSlug : Slug
{
    private TicketedEventSlug(string value) : base(value) { }
    
    public override string ToString() => Value;

    public static ValidationResult<TicketedEventSlug> TryFrom(string? input)
        => NormalizeAndValidate(input)
            .Map(normalized => new TicketedEventSlug(normalized));

    public static TicketedEventSlug From(string input)
        => TryFrom(input).GetValueOrThrow();
}