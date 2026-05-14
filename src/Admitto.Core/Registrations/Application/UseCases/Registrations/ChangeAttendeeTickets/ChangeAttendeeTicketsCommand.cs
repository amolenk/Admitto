using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed record ChangeAttendeeTicketsCommand(
    Guid EventId,
    Guid RegistrationId,
    IReadOnlyList<string> TicketTypeSlugs,
    ChangeMode Mode) : Command;
