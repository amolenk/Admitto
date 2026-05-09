// using Amolenk.Admitto.Core.Registrations.Domain.Entities;
// using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
//
// namespace Amolenk.Admitto.Core.Registrations.Tests.Application.Builders;
//
// public class RegistrationBuilder
// {
//     private TicketedEventId _eventId = new TicketedEventId();
//     private string _firstName = "Firstname";
//     private string _lastName = "Lastname";
//     private EmailAddress _emailAddress = EmailAddress.From("firstname.lastname@example.com");
//     
//     public RegistrationBuilder WithEventId(TicketedEventId eventId)
//     {
//         _eventId = eventId;
//         return this;
//     }
//
//     public Registration Build()
//     {
//         return Registration.Create(_eventId, _emailAddress);
//     }
// }