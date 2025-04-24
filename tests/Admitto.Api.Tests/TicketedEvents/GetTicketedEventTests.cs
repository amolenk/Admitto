// using Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;
// using Amolenk.Admitto.Domain.Entities;
//
// namespace Amolenk.Admitto.Application.Tests.TicketedEvents;
//
// [TestClass]
// public class GetTicketedEventsTests : DistributedAppTestBase
// {
//     [TestMethod]
//     public async Task GetTicketedEvent_EventExists_ReturnsEvent()
//     {
//         // Arrange
//         var nextYear = DateTime.Today.Year + 1;
//         var ticketedEvent = TicketedEvent.Create(
//             "Test Event",
//             new DateOnly(nextYear, 1, 24),
//             new DateOnly(nextYear, 1, 25),
//             DateTime.UtcNow,
//             new DateTime(nextYear, 1, 23, 17, 0, 0, DateTimeKind.Utc));
//     
//         var ticketType = TicketType.Create(
//             "General Admission",
//             new DateTime(nextYear, 1, 24, 9, 0, 0, DateTimeKind.Utc),
//             new DateTime(nextYear, 1, 25, 16, 0, 0, DateTimeKind.Utc),
//             200);
//         
//         ticketedEvent.AddTicketType(ticketType);
//     
//         var team = Team.Create("Acme");
//         team.AddActiveEvent(ticketedEvent);
//         
//         Context.Teams.Add(team);
//     
//         await Context.SaveChangesAsync();
//         
//         // Act
//         var response = await Api.GetAsync($"/teams/{team.Id}/events/{ticketedEvent.Id}");
//     
//         // Assert
//         Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
//     
//         var result = (await response.Content.ReadFromJsonAsync<GetTicketedEventResponse>())!;
//     
//         result.ShouldSatisfyAllConditions(
//             r => r.Name.ShouldBe(ticketedEvent.Name),
//             r => r.StartDay.ShouldBe(ticketedEvent.StartDay),
//             r => r.EndDay.ShouldBe(ticketedEvent.EndDay),
//             r => r.SalesStartDateTime.ShouldBe(ticketedEvent.RegistrationStartDateTime),
//             r => r.SalesEndDateTime.ShouldBe(ticketedEvent.SalesEndDateTime),
//             r => r.TicketTypes.Count().ShouldBe(1),
//             r => r.TicketTypes.First().ShouldSatisfyAllConditions(
//                 tt => tt.Name.ShouldBe(ticketType.Name),
//                 tt => tt.StartDateTime.ShouldBe(ticketType.StartDateTime),
//                 tt => tt.EndDateTime.ShouldBe(ticketType.EndDateTime),
//                 tt => tt.MaxCapacity.ShouldBe(ticketType.MaxCapacity),
//                 tt => tt.RemainingCapacity.ShouldBe(ticketType.MaxCapacity)));
//     }
//
//     [TestMethod]
//     public async Task GetTicketedEvent_EventDoesNotExist_ReturnsNotFound()
//     {
//         // Arrange
//         var team = Team.Create("Acme");
//         Context.Teams.Add(team);
//         
//         await Context.SaveChangesAsync();
//
//         // Act
//         var response = await Api.GetAsync($"/teams/{team.Id}/events/00000000-0000-0000-0000-000000000000");
//
//         // Assert
//         Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
//     }
// }