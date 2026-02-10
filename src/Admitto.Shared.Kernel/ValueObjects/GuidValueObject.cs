using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public interface IGuidValueObject
{
    Guid Value { get; }
}

public static class GuidValueObject
{
    public static ValidationResult<TValueObject> TryFrom<TValueObject>(
        Guid value,
        Func<Guid, TValueObject> factory)
        where TValueObject : IGuidValueObject
        => value == Guid.Empty
            ? Errors.Empty
            : ValidationResult<TValueObject>.Success(factory(value));
    
    private static class Errors
    {
        public static readonly Error Empty = new("guid.empty", "GUID is required.");
    }
}