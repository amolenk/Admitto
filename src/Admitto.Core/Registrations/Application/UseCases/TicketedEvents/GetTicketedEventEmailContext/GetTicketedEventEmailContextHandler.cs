using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;

internal sealed class GetTicketedEventEmailContextHandler(IRegistrationsWriteStore writeStore)
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
                BaseUrl = e.BaseUrl.Value.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketedEvent>());

        var registerLink = $"{fields.BaseUrl.TrimEnd('/')}/register";
        var qrCodeLink = $"{fields.BaseUrl.TrimEnd('/')}/qr-code/{query.RegistrationId}";
        var cancelLink = $"{fields.BaseUrl.TrimEnd('/')}/cancel/{query.RegistrationId}";

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
            registerLink,
            qrCodeLink,
            cancelLink,
            firstName,
            lastName);
    }
}
