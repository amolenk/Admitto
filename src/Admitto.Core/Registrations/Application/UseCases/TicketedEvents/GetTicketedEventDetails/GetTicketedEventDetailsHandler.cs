using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails;

internal sealed class GetTicketedEventDetailsHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : IQueryHandler<GetTicketedEventDetailsQuery, TicketedEventDetailsDto?>
{
    public async ValueTask<TicketedEventDetailsDto?> HandleAsync(
        GetTicketedEventDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.EventId && e.TeamId == query.TeamId, cancellationToken);

        if (ticketedEvent is null) return null;

        var now = timeProvider.GetUtcNow();

        return new TicketedEventDetailsDto(
            ticketedEvent.Id.Value,
            ticketedEvent.TeamId.Value,
            ticketedEvent.Name.Value,
            ticketedEvent.WebsiteUrl.Value.ToString(),
            ticketedEvent.BaseUrl.Value.ToString(),
            ticketedEvent.PublicSlug.Value,
            ticketedEvent.StartsAt,
            ticketedEvent.EndsAt,
            ticketedEvent.TimeZone.Value,
            ticketedEvent.Status,
            ticketedEvent.Version,
            ticketedEvent.IsRegistrationOpen(now),
            ticketedEvent.RegistrationPolicy is null
                ? null
                : new RegistrationPolicyDto(
                    ticketedEvent.RegistrationPolicy.OpensAt,
                    ticketedEvent.RegistrationPolicy.ClosesAt,
                    ticketedEvent.RegistrationPolicy.AllowedEmailDomain),
            ticketedEvent.ReconfirmPolicy is null
                ? null
                : new ReconfirmPolicyDto(
                    ticketedEvent.ReconfirmPolicy.OpensAt,
                    ticketedEvent.ReconfirmPolicy.ClosesAt,
                    (int)ticketedEvent.ReconfirmPolicy.MinEmailInterval.TotalHours,
                    ticketedEvent.ReconfirmPolicy.QuietHoursStart,
                    ticketedEvent.ReconfirmPolicy.QuietHoursEnd),
            new WaitlistPolicyDto(
                ticketedEvent.WaitlistPolicy.QuietHoursStart,
                ticketedEvent.WaitlistPolicy.QuietHoursEnd),
            ticketedEvent.AdditionalDetailSchema.Fields
                .Select(f => new AdditionalDetailFieldDto(f.Key, f.Name, f.MaxLength))
                .ToArray());
    }
}
