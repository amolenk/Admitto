using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for an attendee, based on the e-mail address.
/// </summary>
public record AttendeeId(Guid Value)
{
    public static AttendeeId FromEmail(string email)
    {
        return new AttendeeId(DeterministicGuidGenerator.Generate(email));
    }
}