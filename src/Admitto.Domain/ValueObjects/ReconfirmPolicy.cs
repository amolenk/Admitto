namespace Amolenk.Admitto.Domain.ValueObjects;

public record ReconfirmPolicy(
    TimeSpan WindowStart,
    TimeSpan WindowEnd,
    TimeSpan InitialDelay,
    TimeSpan Frequency)
{
    public static ReconfirmPolicy Default => new(
        TimeSpan.FromDays(21),
        TimeSpan.FromDays(5),
        TimeSpan.Zero, // TODO TimeSpan.FromDays(3),
        TimeSpan.Zero); // TODO FromDays(3));
    
    
    // TODO Log
    
    public bool ShouldSendReconfirmEmail(DateTimeOffset registeredAt, DateTimeOffset? reconfirmationRequestedAt)
    {
        if (reconfirmationRequestedAt is not null
            && DateTimeOffset.UtcNow - reconfirmationRequestedAt > Frequency)
        {
            return true;
        }
        
        if (reconfirmationRequestedAt is null
            && DateTimeOffset.UtcNow - registeredAt > InitialDelay)
        {
            return true;
        }

        return false;
    }
}