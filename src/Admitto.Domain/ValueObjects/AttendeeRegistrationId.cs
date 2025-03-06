using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for an attendee registration.
/// </summary>
public record AttendeeRegistrationId(Guid Value)
{
    public static AttendeeRegistrationId FromAttendeeAndEvent(AttendeeId attendeeId, TicketedEventId ticketedEventId)
    {
        return new AttendeeRegistrationId(
            DeterministicGuidGenerator.Generate($"{attendeeId.Value}:{ticketedEventId.Value}"));
    }
}