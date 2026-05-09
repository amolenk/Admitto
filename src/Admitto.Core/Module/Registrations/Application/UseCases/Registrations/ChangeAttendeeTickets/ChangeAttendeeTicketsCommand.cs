using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed record ChangeAttendeeTicketsCommand(
    Guid EventId,
    Guid RegistrationId,
    IReadOnlyList<string> TicketTypeSlugs,
    ChangeMode Mode) : Command;
