namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public readonly record struct TeamId(Guid Value)
{
    public static TeamId New() => new(Guid.NewGuid());
}