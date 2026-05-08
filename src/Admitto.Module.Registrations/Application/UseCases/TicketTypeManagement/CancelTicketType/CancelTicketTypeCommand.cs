using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType;

internal sealed record CancelTicketTypeCommand(
    Guid EventId,
    string Slug) : Command;
