namespace Amolenk.Admitto.Domain.ValueObjects;

public record CancellationPolicy(TimeSpan LateCancellationTime)
{
    public static CancellationPolicy Default => new(TimeSpan.FromDays(3));
}