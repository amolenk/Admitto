// using System.Text.Json.Serialization;
// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Domain.Entities;
//
// /// <summary>
// /// Represents the registration for an event.
// /// </summary>
// public class AttendeeRegistration : Entity
// {
//     [JsonConstructor]    
//     private AttendeeRegistration(Guid id, Guid ticketedEventId, TicketOrder ticketOrder,
//         AttendeeRegistrationStatus status) : base(id)
//     {
//         TicketedEventId = ticketedEventId;
//         TicketOrder = ticketOrder;
//         Status = status;
//     }
//
//     public Guid TicketedEventId { get; private set; }
//     public TicketOrder TicketOrder { get; private set; }
//     public AttendeeRegistrationStatus Status { get; private set; }
//     
//     public static AttendeeRegistration Create(Guid ticketedEventId, TicketOrder ticketOrder)
//     {
//         return new AttendeeRegistration(Guid.NewGuid(), ticketedEventId, ticketOrder,
//             AttendeeRegistrationStatus.Pending);
//     }
//
//     public void Accept()
//     {
//         // TODO other edge cases?
//         Status = AttendeeRegistrationStatus.Accepted;
//     }
// }
