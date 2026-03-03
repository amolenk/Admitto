// using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
//
// namespace Amolenk.Admitto.Registrations.Domain.ValueObjects;
//
// public readonly record struct RegistrationId : IGuidValueObject
// {
//     public Guid Value { get; }
//     
//     private RegistrationId(Guid value) => Value = value;
//
//     public static RegistrationId New() => new(Guid.NewGuid());
//
//     public static ValidationResult<RegistrationId> TryFrom(Guid value)
//         => GuidValueObject.TryFrom(value, v => new RegistrationId(v), Errors.Empty);
//
//     public static RegistrationId From(Guid value)
//         => GuidValueObject.From(value, v => new RegistrationId(v), Errors.Empty);
//
//     public override string ToString() => Value.ToString();
//
//     private static class Errors
//     {
//         public static readonly Error Empty =
//             new("registration_id.empty", "Registration ID is required.");
//     }
// }