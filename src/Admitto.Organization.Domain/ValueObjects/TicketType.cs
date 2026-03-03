using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public record TicketType(
    Slug Slug,
    DisplayName Name,
    bool IsSelfService, // If false, only admins can grant
    bool IsSelfServiceAvailable, // If false, the ticket type will be shown as unavailable
    TimeSlot[] TimeSlots,
    Capacity? Capacity);