using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;

internal sealed record UpdateTicketTypeCommand(
    Guid EventId,
    string Slug,
    string? Name,
    int? MaxCapacity,
    bool? SelfServiceEnabled = null) : Command;
