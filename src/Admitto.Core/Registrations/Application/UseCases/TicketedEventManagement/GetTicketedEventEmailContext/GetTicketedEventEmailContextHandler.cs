using Amolenk.Admitto.Core.Registrations.Application.Common.Cryptography;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventEmailContext;

internal sealed class GetTicketedEventEmailContextHandler(
    IRegistrationsWriteStore writeStore,
    RegistrationSigner registrationSigner)
    : IQueryHandler<GetTicketedEventEmailContextQuery, TicketedEventEmailContextDto>
{
    public async ValueTask<TicketedEventEmailContextDto> HandleAsync(
        GetTicketedEventEmailContextQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(query.TicketedEventId);

        var fields = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.Id == ticketedEventId)
            .Select(e => new
            {
                Name = e.Name.Value,
                WebsiteUrl = e.WebsiteUrl.Value.ToString(),
                TeamId = e.TeamId.Value,
                EventId = e.Id.Value,
                BaseUrl = e.BaseUrl.Value.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketedEvent>());

        var signature = await registrationSigner.SignAsync(
            query.RegistrationId, ticketedEventId, cancellationToken);

        var qrCodeLink =
            $"{fields.BaseUrl.TrimEnd('/')}/teams/{fields.TeamId}/events/{fields.EventId}" +
            $"/registrations/{query.RegistrationId}/qr-code?signature={signature}";

        string? firstName = null;
        string? lastName = null;

        if (query.RegistrationId != Guid.Empty)
        {
            var registrationId = RegistrationId.From(query.RegistrationId);
            var attendee = await writeStore.Registrations
                .AsNoTracking()
                .Where(r => r.Id == registrationId)
                .Select(r => new { FirstName = r.FirstName.Value, LastName = r.LastName.Value })
                .FirstOrDefaultAsync(cancellationToken);

            firstName = attendee?.FirstName;
            lastName = attendee?.LastName;
        }

        return new TicketedEventEmailContextDto(fields.Name, fields.WebsiteUrl, qrCodeLink, firstName, lastName);
    }
}
