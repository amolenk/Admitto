// using Amolenk.Admitto.Module.Registrations.Application.Persistence;
// using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
// using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
//
// namespace Amolenk.Admitto.Module.Registrations.Application.Services;
//
// public interface ICapacityTracker
// {
//     ValueTask ClaimTicketsAsync(
//         TicketedEventId eventId,
//         IReadOnlyList<Ticket> tickets,
//         CancellationToken cancellationToken = default);
// }
//
// public class CapacityTracker(IRegistrationsWriteStore writeStore) : ICapacityTracker
// {
//     public async ValueTask ClaimTicketsAsync(
//         TicketedEventId eventId,
//         IReadOnlyList<Ticket> tickets,
//         CancellationToken cancellationToken = default)
//     {
//         var eventCapacity = await writeStore.TicketedEventCapacities.FindAsync([eventId], cancellationToken);
//         if (eventCapacity is null)
//         {
//             throw new BusinessRuleViolationException(Errors.EventCapacityNotFound(eventId));
//         }
//         
//         eventCapacity.Claim(ticketRequests);
//     }
//
//     private static class Errors
//     {
//         public static Error EventCapacityNotFound(TicketedEventId eventId) =>
//             new(
//                 "event_capacity_not_found",
//                 "Cannot find capacity details for event.",
//                 ErrorType.Validation,
//                 new Dictionary<string, object?> { ["eventId"] = eventId });
//     }
// }