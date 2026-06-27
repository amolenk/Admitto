using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.PublicEventLinks;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;

internal sealed class GetTicketedEventEmailContextHandler(
    IRegistrationsWriteStore writeStore,
    IOrganizationFacade organizationFacade,
    IOptions<PublicTicketsOptions> publicTicketsOptions)
    : IQueryHandler<GetTicketedEventEmailContextQuery, EventRegistrationSnapshotDto>
{
    public async ValueTask<EventRegistrationSnapshotDto> HandleAsync(
        GetTicketedEventEmailContextQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(query.TicketedEventId);
        var teamId = TeamId.From(query.TeamId);

        var fields = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.Id == ticketedEventId && e.TeamId == teamId)
            .Select(e => new
            {
                Name = e.Name.Value,
                WebsiteUrl = e.WebsiteUrl.Value.ToString(),
                EventId = e.Id.Value,
                BaseUrl = e.BaseUrl.Value.ToString(),
                PublicSlug = e.PublicSlug.Value
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketedEvent>());

        var publicEventLink = $"{publicTicketsOptions.Value.BaseUrl.TrimEnd('/')}/e/{fields.PublicSlug}";
        var registerLink = $"{publicEventLink}/register";
        var qrCodeLink = $"{publicEventLink}/qr-code/{query.RegistrationId}";
        var cancelLink = $"{publicEventLink}/cancel/{query.RegistrationId}";

        var selfServiceTicketCount = await writeStore.TicketCatalogs
            .AsNoTracking()
            .Where(c => c.Id == ticketedEventId && c.TeamId == teamId)
            .Select(c => c.TicketTypes.Count(t => t.SelfServiceEnabled))
            .FirstOrDefaultAsync(cancellationToken);

        var changeTicketsLink = selfServiceTicketCount >= 2
            ? $"{publicEventLink}/registrations/{query.RegistrationId}/tickets"
            : null;

        var branding = await organizationFacade.GetTeamBrandingAsync(teamId.Value, cancellationToken);
        var accentColor = branding?.AccentColor ?? "#2563eb";

        string? firstName = null;
        string? lastName = null;

        if (query.RegistrationId != Guid.Empty)
        {
            var registrationId = RegistrationId.From(query.RegistrationId);
            var attendee = await writeStore.Registrations
                .AsNoTracking()
                .Where(r => r.Id == registrationId && r.EventId == ticketedEventId && r.TeamId == teamId)
                .Select(r => new { FirstName = r.FirstName.Value, LastName = r.LastName.Value })
                .FirstOrDefaultAsync(cancellationToken);

            firstName = attendee?.FirstName;
            lastName = attendee?.LastName;
        }

        return new EventRegistrationSnapshotDto(
            fields.Name,
            fields.WebsiteUrl,
            publicEventLink,
            registerLink,
            qrCodeLink,
            cancelLink,
            accentColor,
            changeTicketsLink,
            firstName,
            lastName);
    }
}
