using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;

internal sealed record AddTicketTypeCommand(
    TicketedEventId EventId,
    Slug Slug,
    DisplayName Name,
    string[] TimeSlots,
    int? MaxCapacity,
    bool SelfServiceEnabled = true) : Command;
