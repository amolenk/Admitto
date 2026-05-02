using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ReleaseTickets;

internal sealed record ReleaseTicketsCommand(
    RegistrationId RegistrationId,
    TicketedEventId TicketedEventId) : Command;
