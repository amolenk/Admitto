using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService;

internal sealed record RegisterAttendeeSelfServiceCommand(
    Guid EventId,
    string Email,
    string FirstName,
    string LastName,
    Guid[] TicketTypeIds,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null) : Command<Guid>;
