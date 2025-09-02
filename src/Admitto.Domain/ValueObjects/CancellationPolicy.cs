namespace Amolenk.Admitto.Domain.ValueObjects;

public record CancellationPolicy(TimeSpan CutoffBeforeEvent)
{
    public static CancellationPolicy Default => new(TimeSpan.FromDays(3));
}