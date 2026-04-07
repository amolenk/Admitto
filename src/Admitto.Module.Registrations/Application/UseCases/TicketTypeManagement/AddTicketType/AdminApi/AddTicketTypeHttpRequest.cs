using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using SlugType = Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.Slug;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType.AdminApi;

public sealed record AddTicketTypeHttpRequest(
    string Slug,
    string Name,
    string[]? TimeSlots = null,
    int? MaxCapacity = null)
{
    internal AddTicketTypeCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        SlugType.From(Slug),
        DisplayName.From(Name),
        TimeSlots ?? [],
        MaxCapacity);
}
