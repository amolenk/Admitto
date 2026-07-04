using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.AddTicketType;

internal sealed record AddTicketTypeCommand(
    Guid EventId,
    Guid TeamId,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    bool SelfServiceEnabled = true,
    bool WaitlistEnabled = false,
    int ClaimWindowHours = 8,
    int? MaxReconfirmAttempts = null) : Command<Guid>;
