// using Amolenk.Admitto.Domain.Entities;
//
// namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Data;
//
// public static class TicketedEventDataFactory
// {
//     public static TicketedEvent CreateTicketedEvent(string? name = null, DateTimeOffset? startDateTime = null,
//         DateTimeOffset? endDateTime = null, DateTimeOffset? registrationStartDateTime = null,
//         DateTimeOffset? registrationEndDateTime = null, IEnumerable<TicketType>? ticketTypes = null)
//     {
//         var nextYear = DateTime.Today.Year + 1;
//         var offset = TimeSpan.Zero;
//         
//         name ??= "Test Event";
//         startDateTime ??= new DateTimeOffset(nextYear, 1, 24, 9, 0, 0, offset);
//         endDateTime ??= new DateTimeOffset(nextYear, 1, 25, 16, 0, 0, offset);
//         registrationStartDateTime ??= DateTimeOffset.UtcNow;
//         registrationEndDateTime ??= new DateTimeOffset(nextYear, 1, 23, 18, 0, 0, offset);
//         ticketTypes ??= [CreateTicketType()];
//         
//         return TicketedEvent.Create(name, startDateTime.Value, endDateTime.Value, 
//             registrationStartDateTime.Value, registrationEndDateTime.Value, ticketTypes);
//     }
//     
//     public static TicketType CreateTicketType(string? name = null, string? slotName = null, int? quantity = null)
//     {
//         name ??= "Test Ticket Type";
//         slotName ??= "Default";
//         quantity ??= 100;
//         
//         return TicketType.Create(name, slotName, quantity.Value);
//     }
// }