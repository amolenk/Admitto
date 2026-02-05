namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public readonly record struct TeamMemberId(Guid Value)
{
    public static TeamMemberId New() => new(Guid.NewGuid());
}