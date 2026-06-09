using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService;

internal sealed record RegisterAttendeeSelfServiceCommand(
    Guid EventId,
    Guid TeamId,
    string Email,
    string FirstName,
    string LastName,
    Guid[] RegisterTicketTypeIds,
    Guid[] WaitlistTicketTypeIds,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null) : Command<RegisterAttendeeSelfServiceResult>;

internal sealed record RegisterAttendeeSelfServiceResult(
    Guid? RegistrationId,
    Guid[] RegisteredTicketTypeIds,
    Guid[] WaitlistedTicketTypeIds);
