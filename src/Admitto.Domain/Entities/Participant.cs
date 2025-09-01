using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a participant of a ticketed event. A participant can either be an attendee
/// or a contributor.
/// </summary>
public class Participant : Aggregate
{
    /// <summary>
    /// EF Core constructors
    /// </summary>
    private Participant()
    {
    }

    private Participant(
        Guid id,
        Guid publicId,
        Guid ticketedEventId,
        string email)
        : base(id)
    {
        PublicId = publicId;
        TicketedEventId = ticketedEventId;
        Email = email;
    }

    public Guid PublicId { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; } = null!;
    
    public static Participant Create(
        Guid eventId,
        string email)
    {
        var privateId = Guid.NewGuid();
        var publicId = DeterministicGuid.Create(privateId.ToString(), eventId);
        
        return new Participant(
            privateId,
            publicId,            
            eventId,
            email);
    }
}