using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType;

internal sealed record CancelTicketTypeCommand(
    TicketedEventId EventId,
    Slug Slug) : Command;
