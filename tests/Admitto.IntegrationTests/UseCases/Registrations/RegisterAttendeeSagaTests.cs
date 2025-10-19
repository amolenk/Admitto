// using Amolenk.Admitto.Application.UseCases.Registrations;
// using Amolenk.Admitto.Domain.DomainEvents;
// using Amolenk.Admitto.Domain.Entities;
//
// namespace Amolenk.Admitto.IntegrationTests.UseCases.Registrations;
//
// [DoNotParallelize]
// [TestClass]
// public class RegisterAttendeeSagaTests : FullStackTestsBase
// {
//     [TestMethod]
//     public async Task RegistrationReceived_SendsConfirmRegistrationEmail()
//     {
//         // Arrange
//         var registrationId = Guid.NewGuid();
//         var domainEvent = new RegistrationReceivedDomainEvent(registrationId);
//         
//         // Act
//         await HandleEvent<RegistrationReceivedDomainEvent, RegisterAttendeeSaga>(domainEvent);
//     
//         // Assert
//         Database.Context.Jobs.ShouldHaveSingleItem().ShouldSatisfyAllConditions(job =>
//         {
//             job.Status.ShouldBe(JobStatus.Pending);
//         });
//         
//         // await QueueStorage.MessageQueue.ShouldContainMessageAsync<HoldTicketsCommand>(message =>
//         // {
//         //     message.RegistrationId.ShouldBe(registrationId);
//         // });
//     }
// }
