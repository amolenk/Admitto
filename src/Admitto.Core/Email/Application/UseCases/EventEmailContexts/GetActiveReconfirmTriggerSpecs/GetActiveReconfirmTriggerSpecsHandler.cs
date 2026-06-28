using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetActiveReconfirmTriggerSpecs;

internal sealed class GetActiveReconfirmTriggerSpecsHandler(IEmailReadStore readStore)
    : IQueryHandler<GetActiveReconfirmTriggerSpecsQuery, IReadOnlyList<ReconfirmTriggerSpecDto>>
{
    public async ValueTask<IReadOnlyList<ReconfirmTriggerSpecDto>> HandleAsync(
        GetActiveReconfirmTriggerSpecsQuery query,
        CancellationToken cancellationToken)
    {
        return await readStore.EventEmailContexts
            .AsNoTracking()
            .Where(c => !c.IsArchived
                        && c.TimeZone != null
                        && c.ReconfirmOpensAt != null
                        && c.ReconfirmClosesAt != null
                        && c.ReconfirmCadenceHours != null
                        && c.ReconfirmMinEmailIntervalHours != null)
            .Select(c => new ReconfirmTriggerSpecDto(
                c.TeamId.Value,
                c.TicketedEventId.Value,
                c.TimeZone!,
                c.ReconfirmOpensAt!.Value,
                c.ReconfirmClosesAt!.Value,
                c.ReconfirmCadenceHours!.Value,
                c.ReconfirmMinEmailIntervalHours!.Value))
            .ToListAsync(cancellationToken);
    }
}
