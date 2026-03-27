using Humanizer;

namespace Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

public static class AlreadyExistsError
{
    public static Error Create<T>(object key)
        => new(
        $"{typeof(T).Name.Kebaberize()}.already_exists",
        $"{typeof(T).Name.Humanize()} already exists.",
        new Dictionary<string, object?>
        {
            ["key"] = key.ToString() ?? string.Empty
        },
        ErrorType.Conflict);
    
    public static Error Create<T>()
        => new(
            $"{typeof(T).Name.Kebaberize()}.already_exists",
            $"{typeof(T).Name.Humanize()} already exists.",
            Type: ErrorType.Conflict);
}