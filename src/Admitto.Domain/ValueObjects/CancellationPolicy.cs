namespace Amolenk.Admitto.Domain.ValueObjects;

public record CancellationPolicy(TimeSpan CutoffBeforeEvent)
{
    public static CancellationPolicy Default => new(TimeSpan.FromDays(3));
    
    public static CancellationPolicy None => new(TimeSpan.Zero);
}