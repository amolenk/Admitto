using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetPublicTicketTypes;

internal sealed class GetPublicTicketTypesHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetPublicTicketTypesQuery, IReadOnlyList<PublicTicketTypeDto>>
{
    public async ValueTask<IReadOnlyList<PublicTicketTypeDto>> HandleAsync(
        GetPublicTicketTypesQuery query,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == query.EventId, cancellationToken);

        if (catalog is null)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketCatalog>(query.EventId.Value));

        return catalog.TicketTypes
            .Where(tt => tt.SelfServiceEnabled)
            .Select(tt => new PublicTicketTypeDto(
                tt.Id.Value,
                tt.Name.Value,
                tt.TimeSlots.Select(ts => ts.Value).ToArray(),
                tt.MaxCapacity,
                tt.UsedCapacity))
            .ToList();
    }
}
