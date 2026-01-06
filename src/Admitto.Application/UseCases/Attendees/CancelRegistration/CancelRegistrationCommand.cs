using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.CancelRegistration;

/// <summary>
/// Represents a command to cancel the registration for an attendee of a ticketed event.
/// </summary>
public record CancelRegistrationCommand(Guid TicketedEventId, Guid AttendeeId) : Command;