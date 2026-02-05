namespace Amolenk.Admitto.Registrations.Domain.ValueObjects;

public readonly record struct RegistrationId(Guid Value) : IGuidValueObject
{
    public static RegistrationId New() => new(Guid.NewGuid());
}