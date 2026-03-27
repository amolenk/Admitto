namespace Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

public class ConcurrencyConflictError
{
    public static Error Create() =>
        new(
            "concurrency_conflict",
            "The resource was modified by another operation.",
            Type: ErrorType.Conflict);
    
    public static Error Create(uint expectedVersion, uint actualVersion) =>
        new(
            "concurrency_conflict",
            "The resource was modified by another operation.",
            new Dictionary<string, object?>
            {
                ["expectedVersion"] = expectedVersion,
                ["actualVersion"] = actualVersion
            },
            ErrorType.Conflict);
}