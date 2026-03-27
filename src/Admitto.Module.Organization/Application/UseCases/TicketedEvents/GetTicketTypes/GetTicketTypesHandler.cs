// using Amolenk.Admitto.Module.Organization.Contracts;
// using Amolenk.Admitto.Module.Organization.Application.Mapping;
// using Amolenk.Admitto.Module.Organization.Application.Persistence;
// using Amolenk.Admitto.Module.Shared.Application.Messaging;
//
// namespace Amolenk.Admitto.Module.Organization.Application.UseCases.GetTicketTypes;
//
// internal class GetTicketTypesHandler(IOrganizationWriteStore writeStore)
//     : IQueryHandler<GetTicketTypesQuery, TicketTypeDto[]>
// {
//     public async ValueTask<TicketTypeDto[]> HandleAsync(
//         GetTicketTypesQuery query,
//         CancellationToken cancellationToken)
//     {
//         var ticketTypes = await writeStore.TicketedEvents
//             .AsNoTracking()
//             .Where(t => t.Id == query.TicketedEventId.Value)
//             .SelectMany(t => t.TicketTypes.Select(tt => tt.ToDto()))
//             .ToArrayAsync(cancellationToken);
//
//         return ticketTypes;
//     }
// }