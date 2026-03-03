// using System.Net;
// using System.Net.Http.Json;
// using Amolenk.Admitto.Api.Tests.Infrastructure;
// using Amolenk.Admitto.Testing.Builders;
// using Amolenk.Admitto.Testing.Infrastructure.Assertions;
// using Shouldly;
//
// namespace Amolenk.Admitto.Api.Tests.Registrations.RegisterAttendee;
//
// [TestClass]
// public sealed class ApiTests(TestContext testContext) : EndToEndTestBase
// {
//     [TestMethod]
//     public async Task RegisterAttendee_HappyFlow_Returns201Created()
//     {
//         // Arrange
//         var fixture = RegisterAttendeeFixture.HappyFlow();
//         await fixture.SetupAsync(Environment);
//
//         var request = new RegisterAttendeeRequestBuilder()
//             .WithTicketRequests([fixture.TicketTypeId])
//             .Build();
//
//         // Act
//         var response = await Environment.ApiClient.PostAsJsonAsync(
//             RegisterAttendeeFixture.Route,
//             request,
//             cancellationToken: testContext.CancellationToken);
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.Created);
//     }
//
//     [TestMethod]
//     public async Task RegisterAttendee_SoldOut_Returns400Error()
//     {
//         // Arrange
//         var fixture = RegisterAttendeeFixture.SoldOut();
//         await fixture.SetupAsync(Environment);
//
//         var request = new RegisterAttendeeRequestBuilder()
//             .WithTicketRequests([fixture.TicketTypeId])
//             .Build();
//
//         // Act
//         var response = await Environment.ApiClient.PostAsJsonAsync(
//             RegisterAttendeeFixture.Route,
//             request,
//             cancellationToken: testContext.CancellationToken);
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
//         response.Content.ShouldBeProblemDetails().ShouldHaveErrorCode("ticket_type_sold_out");
//     }
// }
