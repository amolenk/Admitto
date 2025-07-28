namespace Amolenk.Admitto.Domain.ValueObjects;

public enum RegistrationType
{
    /// <summary>
    /// External registrations are those that come from outside the system, such as through a public registration form.
    /// Attendees are required to verify their identity before they can be registered and are subject to ticket limits.
    /// Attendees may need to reconfirm their attendance if asked to do so by the organizer.
    /// </summary>
    Public,
    
    /// <summary>
    /// OnBehalfOf registrations are those that are created by an organizer on behalf of an attendee.
    /// Attendees are not required to verify their identity and are not subject to ticket limits.
    /// Attendees may need to reconfirm their attendance if asked to do so by the organizer.
    /// </summary>
    OnBehalfOf,
    
    /// <summary>
    /// Internal registrations are those that are created within the system, such as by an event organizer.
    /// Attendees are not required to verify their identity and are not subject to ticket limits.
    /// Attendees never need to reconfirm their attendance.
    /// </summary>
    Internal
}
