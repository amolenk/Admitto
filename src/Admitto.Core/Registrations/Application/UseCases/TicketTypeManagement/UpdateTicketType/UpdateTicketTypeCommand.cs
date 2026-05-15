using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;

internal sealed record UpdateTicketTypeCommand(
    Guid EventId,
    Guid TicketTypeId,
    string? Name,
    int? MaxCapacity,
    bool? SelfServiceEnabled = null) : Command;
