using Amolenk.Admitto.Module.Registrations.Application.Common.Cryptography;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;

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
                TeamSlug = e.TeamSlug.Value,
                EventSlug = e.Slug.Value,
                BaseUrl = e.BaseUrl.Value.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketedEvent>(query.TicketedEventId));

        var signature = await registrationSigner.SignAsync(
            query.RegistrationId, ticketedEventId, cancellationToken);

        var qrCodeLink =
            $"{fields.BaseUrl.TrimEnd('/')}/teams/{fields.TeamSlug}/events/{fields.EventSlug}" +
            $"/registrations/{query.RegistrationId}/qr-code?signature={signature}";

        return new TicketedEventEmailContextDto(fields.Name, fields.WebsiteUrl, qrCodeLink);
    }
}
