using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public interface IInt32ValueObject
{
    int Value { get; }
}

public static class Int32ValueObject
{
    public static ValidationResult<TValueObject> TryFrom<TValueObject>(
        int value,
        int minValue,
        int maxValue,
        Func<int, TValueObject> factory)
        where TValueObject : IInt32ValueObject
        => value < minValue || value > maxValue
            ? Errors.OutOfRange(minValue, maxValue)
            : ValidationResult<TValueObject>.Success(factory(value));
    
    // TODO Make all value object TryFrom methods use standard error messages
    private static class Errors
    {
        public static Error OutOfRange(int minValue, int maxValue) => new(
            "out_of_range",
            $"Value must be between {minValue} and {maxValue}.");
    }
}