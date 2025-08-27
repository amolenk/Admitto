namespace Amolenk.Admitto.Domain.ValueObjects;

public record ReconfirmationPolicy(
    TimeSpan WindowStart,
    TimeSpan WindowEnd,
    TimeSpan RegistrationInterval,
    TimeSpan RepeatInterval,
    int MaxAttempts)
{
    public static ReconfirmationPolicy Default => new(
        TimeSpan.FromDays(21),
        TimeSpan.FromDays(5),
        TimeSpan.FromDays(3),
        TimeSpan.FromDays(3),
        3);
}