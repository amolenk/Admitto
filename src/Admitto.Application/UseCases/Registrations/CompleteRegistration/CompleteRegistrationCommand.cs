using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;

/// <summary>
/// Represents a command to complete the registration of an attendee for a ticketed event.
/// </summary>
public record CompleteRegistrationCommand(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    string Email,
    string FirstName,
    string LastName,
    IList<AdditionalDetail> AdditionalDetails,
    IList<TicketSelection> Tickets)
    : Command;
