using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public record TeamSlug : Slug
{
    private TeamSlug(string value) : base(value) { }
    
    public override string ToString() => Value;

    public static ValidationResult<TeamSlug> TryFrom(string? input)
        => NormalizeAndValidate(input)
            .Map(normalized => new TeamSlug(normalized));

    public static TeamSlug From(string input)
        => TryFrom(input).GetValueOrThrow();
}