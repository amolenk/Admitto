using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed record ChangeAttendeeTicketsCommand(
    Guid EventId,
    Guid RegistrationId,
    IReadOnlyList<Guid> TicketTypeIds,
    ChangeMode Mode) : Command;
