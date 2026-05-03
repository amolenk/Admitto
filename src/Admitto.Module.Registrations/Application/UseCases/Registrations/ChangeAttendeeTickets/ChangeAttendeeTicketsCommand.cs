using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed record ChangeAttendeeTicketsCommand(
    TicketedEventId EventId,
    RegistrationId RegistrationId,
    IReadOnlyList<string> TicketTypeSlugs,
    ChangeMode Mode) : Command;
