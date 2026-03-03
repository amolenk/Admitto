// using Amolenk.Admitto.Organization.Application.Persistence;
// using Amolenk.Admitto.Organization.Domain.ValueObjects;
// using Amolenk.Admitto.Shared.Application.Messaging;
// using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
// using Amolenk.Admitto.Shared.Kernel.ValueObjects;
//
// namespace Amolenk.Admitto.Organization.Application.UseCases.GetEventId;
//
// internal class GetEventIdHandler(IOrganizationWriteStore writeStore)
//     : IQueryHandler<GetEventIdQuery, TicketedEventId>
// {
//     public async ValueTask<TicketedEventId> HandleAsync(
//         GetEventIdQuery query,
//         CancellationToken cancellationToken)
//     {
//         var ticketedEventId = await writeStore.TicketedEvents
//             .AsNoTracking()
//             .Where(e => e.TeamId == query.TeamId && e.Slug == query.EventSlug)
//             .Select(e => (TicketedEventId?)e.Id)
//             .FirstOrDefaultAsync(cancellationToken);
//
//         return ticketedEventId ??
//                throw new BusinessRuleViolationException(Errors.TicketedEventNotFound(query.TeamId, query.EventSlug));
//     }
//
//     private static class Errors
//     {
//         public static Error TicketedEventNotFound(TeamId teamId, Slug eventSlug) =>
//             new(
//                 "event.not_found",
//                 "Event could not be found.",
//                 Details: new Dictionary<string, object?>
//                 {
//                     ["teamId"] = teamId,
//                     ["eventSlug"] = eventSlug
//                 });
//     }
// }