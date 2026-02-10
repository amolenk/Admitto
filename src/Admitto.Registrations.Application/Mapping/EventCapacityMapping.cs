using Amolenk.Admitto.Registrations.Application.Persistence;
using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Shared.Application.Mapping;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.Mapping;

internal static class EventCapacityMapping
{
    public static EventCapacity ToDomain(this EventCapacityRecord record) => EventCapacity.Rehydrate(
        new TicketedEventId(record.EventId),
        record.TicketCapacities.Select(TicketCapacityMapping.ToDomain).ToList());

    extension(EventCapacity eventCapacity)
    {
        public EventCapacityRecord ToRecord() => new()
        {
            EventId = eventCapacity.Id.Value,
            TicketCapacities = eventCapacity.TicketCapacities.Select(TicketCapacityMapping.ToRecord).ToList()
        };

        public void ApplyToRecord(EventCapacityRecord record)
        {
            record.EventId = eventCapacity.Id.Value;

            record.TicketCapacities.ApplyToRecords(
                eventCapacity.TicketCapacities,
                domainItem => domainItem.Id.Value,
                recordItem => recordItem.TicketTypeId,
                domainItem => domainItem.ToRecord(),
                (domainItem, recordItem) => domainItem.ApplyToRecord(recordItem)
            );
        }
    }
}