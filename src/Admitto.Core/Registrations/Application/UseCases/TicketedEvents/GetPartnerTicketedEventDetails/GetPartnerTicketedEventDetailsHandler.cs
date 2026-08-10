using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetPartnerTicketedEventDetails;

internal sealed class GetPartnerTicketedEventDetailsHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : IQueryHandler<GetPartnerTicketedEventDetailsQuery, PartnerTicketedEventDetailsDto?>
{
    public async ValueTask<PartnerTicketedEventDetailsDto?> HandleAsync(
        GetPartnerTicketedEventDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.EventId && e.TeamId == query.TeamId, cancellationToken);

        if (ticketedEvent is null) return null;

        var now = timeProvider.GetUtcNow();

        return new PartnerTicketedEventDetailsDto(
            ticketedEvent.Name.Value,
            ticketedEvent.PublicSlug.Value,
            ticketedEvent.StartsAt,
            ticketedEvent.EndsAt,
            ticketedEvent.TimeZone.Value,
            ticketedEvent.IsRegistrationOpen(now),
            ticketedEvent.RegistrationPolicy?.AllowedEmailDomain,
            ticketedEvent.AdditionalDetailSchema.Fields
                .Select(f => new PartnerAdditionalDetailFieldDto(f.Key, f.Name, f.MaxLength))
                .ToArray());
    }
}
