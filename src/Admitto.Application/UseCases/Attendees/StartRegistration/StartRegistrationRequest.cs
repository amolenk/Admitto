using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.StartRegistration;

public record StartRegistrationRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetail> AdditionalDetails,
    List<TicketSelection> Tickets,
    bool IsInvited);