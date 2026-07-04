using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;

internal sealed record UpdateTicketTypeCommand(
    Guid EventId,
    Guid TeamId,
    Guid TicketTypeId,
    string? Name,
    int? MaxCapacity,
    bool? SelfServiceEnabled = null,
    bool? WaitlistEnabled = null,
    int? ClaimWindowHours = null,
    int? MaxReconfirmAttempts = null,
    bool UpdateMaxReconfirmAttempts = false) : Command;
