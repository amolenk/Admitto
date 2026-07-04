using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReleaseTickets;

internal sealed record ReleaseTicketsCommand(
    Guid RegistrationId,
    Guid TicketedEventId,
    Guid TeamId) : Command;
