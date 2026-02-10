using Amolenk.Admitto.Registrations.Application.Persistence;
using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.Mapping;

internal static class TicketCapacityMapping
{
    public static TicketCapacity ToDomain(this TicketCapacityRecord record) => TicketCapacity.Rehydrate(
        new TicketTypeId(record.TicketTypeId),
        record.MaxCapacity,
        record.UsedCapacity);

    extension(TicketCapacity ticketCapacity)
    {
        public TicketCapacityRecord ToRecord() => new()
        {
            TicketTypeId = ticketCapacity.Id.Value,
            MaxCapacity = ticketCapacity.MaxCapacity,
            UsedCapacity = ticketCapacity.UsedCapacity
        };

        public void ApplyToRecord(TicketCapacityRecord record)
        {
            record.TicketTypeId = ticketCapacity.Id.Value;
            record.UsedCapacity = ticketCapacity.UsedCapacity;
            record.MaxCapacity = ticketCapacity.MaxCapacity;
        }
    }
}