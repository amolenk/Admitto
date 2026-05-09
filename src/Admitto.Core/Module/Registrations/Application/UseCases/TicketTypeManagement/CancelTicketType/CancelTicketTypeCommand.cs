using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType;

internal sealed record CancelTicketTypeCommand(
    Guid EventId,
    string Slug) : Command;
