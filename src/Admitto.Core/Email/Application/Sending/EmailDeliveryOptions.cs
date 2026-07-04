namespace Amolenk.Admitto.Core.Email.Application.Sending;

internal sealed class EmailDeliveryOptions
{
    public int InlineRetryCount { get; init; } = 2;
    public TimeSpan InlineRetryDelay { get; init; } = TimeSpan.FromMilliseconds(250);
    public int MaxDeliveryAttempts { get; init; } = 5;
}
