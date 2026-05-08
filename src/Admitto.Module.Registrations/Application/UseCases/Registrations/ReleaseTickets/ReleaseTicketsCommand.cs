using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ReleaseTickets;

internal sealed record ReleaseTicketsCommand(
    Guid RegistrationId,
    Guid TicketedEventId) : Command;
