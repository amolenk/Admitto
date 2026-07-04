using Humanizer;

namespace Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

public static class NotFoundError
{
    public static Error Create<T>()
        => new(
        $"{typeof(T).Name.Kebaberize()}.not_found",
        $"{typeof(T).Name.Humanize()} not found.",
        Type: ErrorType.NotFound);
}
