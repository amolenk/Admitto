using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Registrations.Application.Mapping;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.Persistence;

public static class OrganizationWriteStoreExtensions
{
    public static async ValueTask<TicketedEvent> RehydrateTicketedEventAsync(
        this IOrganizationWriteStore writeStore,
        TicketedEventId id,
        CancellationToken cancellationToken = default)
    {
        var record = await writeStore.TicketedEvents.FindAsync([id], cancellationToken);
        
        return record is not null
            ? record.ToDomain()
            : throw new BusinessRuleViolationException(Errors.TicketedEventNotFound(id));
    }

    private static class Errors
    {
        public static Error TicketedEventNotFound(TicketedEventId id) =>
            new(
                "org.event_not_found",
                "Event could not be found.",
                Details: new Dictionary<string, object?> { ["eventId"] = id });
    }
}