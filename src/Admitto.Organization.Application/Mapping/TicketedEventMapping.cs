using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.Mapping;

internal static class TicketedEventMapping
{
    public static TicketedEvent ToDomain(this TicketedEventRecord record) => TicketedEvent.Rehydrate(
        new TicketedEventId(record.Id),
        new TeamId(record.TeamId),
        TicketedEventSlug.From(record.Slug),
        record.Name,
        record.Website,
        record.BaseUrl,
        record.StartsAt,
        record.EndsAt,
        record.TicketTypes
            .Select(tt => new TicketType(
                new TicketTypeId(tt.Id),
                tt.AdminLabel,
                tt.PublicTitle,
                tt.IsSelfService,
                tt.IsSelfServiceAvailable,
                tt.TimeSlots.ToArray(),
                tt.Capacity))
            .ToArray());

    public static TicketedEventRecord ToRecord(this TicketedEvent ticketedEvent) => new()
    {
        Id = ticketedEvent.Id.Value,
        TeamId = ticketedEvent.TeamId.Value,
        Slug = ticketedEvent.Slug.Value,
        Name = ticketedEvent.Name,
        Website = ticketedEvent.Website,
        BaseUrl = ticketedEvent.BaseUrl,
        StartsAt = ticketedEvent.StartsAt,
        EndsAt = ticketedEvent.EndsAt,
        TicketTypes = ticketedEvent.TicketTypes
            .Select(tt => new TicketTypeRecord()
            {
                Id = tt.Id.Value,
                AdminLabel = tt.AdminLabel,
                PublicTitle = tt.PublicTitle,
                IsSelfService = tt.IsSelfService,
                IsSelfServiceAvailable = tt.IsSelfServiceAvailable,
                TimeSlots = tt.TimeSlots.ToList(),
                Capacity = tt.Capacity
            }).ToList()
    };
}