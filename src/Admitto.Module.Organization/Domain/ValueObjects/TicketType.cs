using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.ValueObjects;

public record TicketType(
    Slug Slug,
    DisplayName Name,
    TimeSlot[] TimeSlots,
    Capacity? Capacity,
    bool IsCancelled = false);