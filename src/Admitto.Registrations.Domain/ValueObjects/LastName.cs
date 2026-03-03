// using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
//
// namespace Amolenk.Admitto.Registrations.Domain.ValueObjects;
//
// public readonly record struct LastName : IStringValueObject
// {
//     private const int MinLength = 1;
//     private const int MaxLength = 100;
//
//     public string Value { get; }
//
//     private LastName(string value) => Value = value;
//
//     public static ValidationResult<LastName> TryFrom(string? value)
//         => StringValueObject.TryFrom(
//             value,
//             MinLength,
//             MaxLength,
//             v => new LastName(v),
//             Errors.Empty,
//             Errors.TooShort,
//             Errors.TooLong);
//
//     public static LastName From(string? value)
//         => StringValueObject.From(
//             value,
//             MinLength,
//             MaxLength,
//             v => new LastName(v),
//             Errors.Empty,
//             Errors.TooShort,
//             Errors.TooLong);
//
//     public override string ToString() => Value;
//
//     private static class Errors
//     {
//         public static readonly Error Empty = new(
//             "last_name.empty",
//             "Last name is required.");
//
//         public static readonly Error TooShort = new(
//             "last_name.too_short",
//             $"Last name must be at least {MinLength} character(s).");
//
//         public static readonly Error TooLong = new(
//             "last_name.too_long",
//             $"Last name must be at most {MaxLength} character(s).");
//     }
// }