using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.Mapping;

internal static class TicketRequestMapping
{
    public static TicketRequest ToPrivilegedTicketRequest(Guid id) => new(
            new TicketTypeId(id),
            TicketGrantMode.Privileged,
            CapacityEnforcementMode.Ignore);
}