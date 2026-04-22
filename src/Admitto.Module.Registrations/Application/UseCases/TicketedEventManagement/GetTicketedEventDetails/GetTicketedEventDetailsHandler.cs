using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails;

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
            .FirstOrDefaultAsync(e => e.Id == query.EventId, cancellationToken);

        if (ticketedEvent is null) return null;

        var now = timeProvider.GetUtcNow();

        return new TicketedEventDetailsDto(
            ticketedEvent.Id.Value,
            ticketedEvent.TeamId.Value,
            ticketedEvent.Slug.Value,
            ticketedEvent.Name.Value,
            ticketedEvent.WebsiteUrl.Value.ToString(),
            ticketedEvent.BaseUrl.Value.ToString(),
            ticketedEvent.StartsAt,
            ticketedEvent.EndsAt,
            ticketedEvent.Status,
            ticketedEvent.Version,
            ticketedEvent.IsRegistrationOpen(now),
            ticketedEvent.RegistrationPolicy is null
                ? null
                : new RegistrationPolicyDto(
                    ticketedEvent.RegistrationPolicy.OpensAt,
                    ticketedEvent.RegistrationPolicy.ClosesAt,
                    ticketedEvent.RegistrationPolicy.AllowedEmailDomain),
            ticketedEvent.CancellationPolicy is null
                ? null
                : new CancellationPolicyDto(ticketedEvent.CancellationPolicy.LateCancellationCutoff),
            ticketedEvent.ReconfirmPolicy is null
                ? null
                : new ReconfirmPolicyDto(
                    ticketedEvent.ReconfirmPolicy.OpensAt,
                    ticketedEvent.ReconfirmPolicy.ClosesAt,
                    (int)ticketedEvent.ReconfirmPolicy.Cadence.TotalDays));
    }
}
