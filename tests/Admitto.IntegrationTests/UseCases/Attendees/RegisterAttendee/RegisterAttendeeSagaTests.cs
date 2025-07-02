// using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;
// using Amolenk.Admitto.Application.UseCases.Registrations;
// using Amolenk.Admitto.Domain.DomainEvents;
//
// namespace Amolenk.Admitto.IntegrationTests.UseCases.Attendees.RegisterAttendee;
//
// [DoNotParallelize]
// [TestClass]
// public class RegisterAttendeeSagaTests : FullStackTestsBase
// {
//     [TestMethod]
//     public async Task RegistrationReceived_SendsReserveTicketsCommand()
//     {
//         // Arrange
//         var registrationId = Guid.NewGuid();
//         var domainEvent = new RegistrationReceivedDomainEvent(registrationId);
//         
//         // Act
//         await HandleEvent<RegistrationReceivedDomainEvent, RegisterAttendeeSaga>(domainEvent);
//     
//         // Assert
//         await QueueStorage.MessageQueue.ShouldContainMessageAsync<HoldTicketsCommand>(message =>
//         {
//             message.RegistrationId.ShouldBe(registrationId);
//         });
//     }
// }
//
