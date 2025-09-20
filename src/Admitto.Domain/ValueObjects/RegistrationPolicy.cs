namespace Amolenk.Admitto.Domain.ValueObjects;

public record RegistrationPolicy(TimeSpan OpensBeforeEvent, TimeSpan ClosesBeforeEvent, string? EmailDomainName = null)
{
    public static RegistrationPolicy Default => new(TimeSpan.FromDays(180), TimeSpan.FromDays(1));
}