using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PartnerApi;

public sealed class RegisterAttendeeSelfServiceTicketStateConflictProblemDetails : ProblemDetails
{
    public string Code { get; init; } = "registration.ticket_state_conflict";
    public Guid[] RegisterableTicketTypeIds { get; init; } = [];
    public Guid[] WaitlistableTicketTypeIds { get; init; } = [];
    public Guid[] UnavailableTicketTypeIds { get; init; } = [];
    public Guid[] UnknownTicketTypeIds { get; init; } = [];
    public Guid[] InvalidForRequestedActionTicketTypeIds { get; init; } = [];
}
