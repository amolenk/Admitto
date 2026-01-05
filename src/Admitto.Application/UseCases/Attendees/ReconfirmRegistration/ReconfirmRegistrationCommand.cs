using Amolenk.Admitto.Application.Common.Messaging;

namespace Amolenk.Admitto.Application.UseCases.Attendees.ReconfirmRegistration;

/// <summary>
/// Represents a command to reconfirm the registration for an attendee of a ticketed event.
/// </summary>
public record ReconfirmRegistrationCommand(Guid TicketedEventId, Guid AttendeeId) : Command;