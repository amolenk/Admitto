using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;

internal sealed record AddTicketTypeCommand(
    Guid EventId,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    bool SelfServiceEnabled = true,
    bool WaitlistEnabled = false,
    int ClaimWindowHours = 8,
    int? MaxReconfirmAttempts = null) : Command<Guid>;
