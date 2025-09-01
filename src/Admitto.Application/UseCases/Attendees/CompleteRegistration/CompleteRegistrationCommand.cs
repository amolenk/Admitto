using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.CompleteRegistration;

/// <summary>
/// Represents a command to complete the registration process for an attendee of a ticketed event.
/// </summary>
public record CompleteRegistrationCommand(
    Guid TicketedEventId,
    Guid ParticipantId,
    string Email,
    string FirstName,
    string LastName,
    IList<AdditionalDetail> AdditionalDetails,
    IList<TicketSelection> ClaimedTickets)
    : Command;