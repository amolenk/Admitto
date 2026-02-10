using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Registrations.Application.Mapping;
using Amolenk.Admitto.Shared.Application.Persistence;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Registrations.Application.Persistence;

public static class EventCapacityPersistenceExtensions
{
    extension(DbSet<EventCapacityRecord> eventCapacityRecords)
    {
        public async ValueTask<EventCapacity> LoadAggregateAsync(
            TicketedEventId id,
            CancellationToken cancellationToken = default)
        {
            var record = await eventCapacityRecords.FindAsync([id], cancellationToken);

            return record is not null
                ? record.ToDomain()
                : throw new BusinessRuleViolationException(Errors.EventCapacityNotFound(id));
        }

        public void SaveAggregate(EventCapacity eventCapacity)
        {
            eventCapacityRecords.ApplyDomainChanges(
                eventCapacity,
                ec => ec.Id.Value,
                (domain, record) => domain.ApplyToRecord(record),
                domain => domain.ToRecord());
        }
    }

    private static class Errors
    {
        public static Error EventCapacityNotFound(TicketedEventId id) =>
            new(
                "event_capacity_not_found",
                "Event capacity could not be found.",
                Details: new Dictionary<string, object?> { ["eventId"] = id });
    }
}