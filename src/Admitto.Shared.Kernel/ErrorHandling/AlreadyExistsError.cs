using Humanizer;

namespace Amolenk.Admitto.Shared.Kernel.ErrorHandling;

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
        ErrorType.NotFound);
}