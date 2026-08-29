namespace Amolenk.Admitto.Core.Email.Application.Sending;

public sealed class EmailDeliveryOptions
{
    public TimeSpan PerMessageDelay { get; init; } = TimeSpan.FromMilliseconds(500);
    public int InlineRetryCount { get; init; } = 2;
    public TimeSpan InlineRetryDelay { get; init; } = TimeSpan.FromMilliseconds(250);
    public int MaxDeliveryAttempts { get; init; } = 5;
}
