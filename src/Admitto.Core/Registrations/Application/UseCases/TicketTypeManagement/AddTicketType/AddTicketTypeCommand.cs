using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;

internal sealed record AddTicketTypeCommand(
    Guid EventId,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    bool SelfServiceEnabled = true) : Command<Guid>;
