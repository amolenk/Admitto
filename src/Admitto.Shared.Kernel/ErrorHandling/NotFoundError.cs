using Humanizer;

namespace Amolenk.Admitto.Shared.Kernel.ErrorHandling;

public static class NotFoundError
{
    public static Error Create<T>(object key)
        => new(
        $"{typeof(T).Name.Kebaberize()}.not_found",
        $"{typeof(T).Name.Humanize()} not found.",
        new Dictionary<string, object?>
        {
            ["key"] = key.ToString() ?? string.Empty
        },
        ErrorType.NotFound);
}