namespace Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

public class CommonErrors
{
    public static Error ConcurrencyConflict() =>
        new(
            "concurrency_conflict",
            "The resource was modified by another operation.",
            Type: ErrorType.Conflict);

    public static Error ConcurrencyConflict(uint expectedVersion, uint actualVersion) =>
        new(
            "concurrency_conflict",
            "The resource was modified by another operation.",
            new Dictionary<string, object?>
            {
                ["expectedVersion"] = expectedVersion,
                ["actualVersion"] = actualVersion
            },
            ErrorType.Conflict);

    public static readonly Error TextEmpty = new(
      "text.empty",
      "Text is required.");

    public static Error TextTooLong(int maxLength) => new(
      "text.too_long",
      $"Text must be at most {maxLength} character(s).");
}
