namespace Amolenk.Admitto.Domain.ValueObjects;

public record RegistrationPolicy(string? EmailDomainName = null)
{
    public static RegistrationPolicy Default => new();
}