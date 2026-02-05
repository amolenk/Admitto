using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public record TicketTypeSlug : Slug
{
    private TicketTypeSlug(string value) : base(value) { }
    
    public override string ToString() => Value;

    public static ValidationResult<TicketTypeSlug> TryFrom(string? input)
        => NormalizeAndValidate(input)
            .Map(normalized => new TicketTypeSlug(normalized));

    public static TicketTypeSlug From(string input)
        => TryFrom(input).GetValueOrThrow();
}