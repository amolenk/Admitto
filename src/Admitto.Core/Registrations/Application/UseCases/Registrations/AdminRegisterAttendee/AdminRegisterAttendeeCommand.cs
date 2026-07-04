using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee;

internal sealed record AdminRegisterAttendeeCommand(
    Guid EventId,
    Guid TeamId,
    string Email,
    string FirstName,
    string LastName,
    Guid[] TicketTypeIds,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null) : Command<Guid>;
