using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;

internal sealed class GetTicketedEventEmailContextHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetTicketedEventEmailContextQuery, TicketedEventEmailContextDto>
{
    public async ValueTask<TicketedEventEmailContextDto> HandleAsync(
        GetTicketedEventEmailContextQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(query.TicketedEventId);

        var dto = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.Id == ticketedEventId)
            .Select(e => new TicketedEventEmailContextDto(
                e.Name.Value,
                e.WebsiteUrl.Value.ToString()))
            .FirstOrDefaultAsync(cancellationToken);

        return dto ?? throw new BusinessRuleViolationException(
            NotFoundError.Create<TicketedEvent>(query.TicketedEventId));
    }
}
