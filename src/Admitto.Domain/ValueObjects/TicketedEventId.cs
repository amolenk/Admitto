using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.ValueObjects;

// TODO Remove

/// <summary>
/// Represents a unique identifier for a ticketed event.
/// </summary>
public record TicketedEventId(Guid Value)
{
    public static TicketedEventId FromTeamIdAndSlug(TeamId teamId, string slug)
    {
        return new TicketedEventId(DeterministicGuid.Create(slug, teamId));
    }
    
    public static implicit operator TicketedEventId(Guid value) => new(value);
    
    public static implicit operator Guid(TicketedEventId ticketedEventId) => ticketedEventId.Value;
}