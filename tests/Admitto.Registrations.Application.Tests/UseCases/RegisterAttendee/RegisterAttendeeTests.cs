// using Amolenk.Admitto.Organization.Contracts;
// using Amolenk.Admitto.Registrations.Application.Persistence;
// using Amolenk.Admitto.Registrations.Application.Services;
// using Amolenk.Admitto.Registrations.Application.Tests.Aspire;
// using Amolenk.Admitto.Registrations.Application.UseCases.RegisterAttendee;
// using Amolenk.Admitto.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Shared.Kernel.ValueObjects;
// using Microsoft.EntityFrameworkCore;
// using Shouldly;
//
// namespace Amolenk.Admitto.Registrations.Application.Tests.UseCases.RegisterAttendee;
//
// [TestClass]
// public sealed class RegisterAttendeeTests(TestContext testContext) : AspireIntegrationTestBase
// {
//     [TestMethod]
//     public async ValueTask RegisterAttendee_HappyFlow_CreatesRegistration()
//     {
//         // Arrange
//         var fixture = RegisterAttendeeFixture.HappyFlow();
//         await fixture.SetupAsync(Environment);
//
//         var command = NewRegisterAttendeeCommand(fixture.EventId, NewTicketRequest(fixture.TicketTypeId));
//         var sut = NewRegisterAttendeeHandler(fixture.OrganizationFacade, Environment.Database.Context);
//
//         // Act
//         var result = await sut.HandleAsync(command, testContext.CancellationToken);
//
//         // Assert
//         await Environment.Database.WithContextAsync(async dbContext =>
//         {
//             // Verify that a registration with one ticket has been created.
//             var registration = await dbContext.Registrations.FindAsync(
//                 [result],
//                 testContext.CancellationToken);
//
//             registration.ShouldNotBeNull().Tickets.Count.ShouldBe(1);
//
//             // Verify that used capacity has been incremented.
//             var ticketedEventCapacity = await dbContext.TicketedEventCapacities.FirstOrDefaultAsync(
//                 testContext.CancellationToken);
//
//             ticketedEventCapacity
//                 .ShouldNotBeNull()
//                 .TicketCapacities.ShouldHaveSingleItem().UsedCapacity.ShouldBe(1);
//         });
//     }
//
//     private static RegisterAttendeeHandler NewRegisterAttendeeHandler(
//         IOrganizationFacade organizationFacade,
//         IRegistrationsWriteStore registrationsWriteModel)
//     {
//         var capacityTracker = new CapacityTracker(registrationsWriteModel);
//
//         return new RegisterAttendeeHandler(capacityTracker, organizationFacade, registrationsWriteModel);
//     }
//
//     private static TicketRequest NewTicketRequest(
//         TicketTypeId? id = null,
//         TicketGrantMode? grantMode = null,
//         CapacityEnforcementMode? capacityEnforcementMode = null)
//     {
//         id ??= TicketTypeId.New();
//         grantMode ??= TicketGrantMode.Privileged;
//         capacityEnforcementMode ??= CapacityEnforcementMode.Enforce;
//
//         return new TicketRequest(id.Value, grantMode.Value, capacityEnforcementMode.Value);
//     }
//
//     private static RegisterAttendeeCommand NewRegisterAttendeeCommand(
//         TicketedEventId eventId,
//         TicketRequest ticketRequest,
//         EmailAddress? emailAddress = null)
//     {
//         emailAddress ??= EmailAddress.From("alice@example.com");
//
//         return new RegisterAttendeeCommand(
//             eventId,
//             "firstName",
//             "lastName",
//             emailAddress.Value,
//             [ticketRequest]);
//     }
// }