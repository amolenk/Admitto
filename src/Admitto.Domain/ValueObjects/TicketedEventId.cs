using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for a ticketed event.
/// </summary>
public record TicketedEventId(Guid Value)
{
    public static TicketedEventId FromTeamIdAndName(TeamId teamId, string name)
    {
        return new TicketedEventId(DeterministicGuidGenerator.Generate($"{teamId.Value}:{name}"));
    }
    
    public static implicit operator TicketedEventId(Guid value) => new(value);
    
    public static implicit operator Guid(TicketedEventId ticketedEventId) => ticketedEventId.Value;
}