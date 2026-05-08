using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;

internal sealed record AddTicketTypeCommand(
    Guid EventId,
    string Slug,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    bool SelfServiceEnabled = true) : Command;
