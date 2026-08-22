using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.ChangeAttendeeTickets;

[TestClass]
public sealed class ChangeAttendeeTicketsTests(TestContext testContext) : EndToEndTestBase
{
    // Authenticated organizer changes ticket types — returns 200 OK
    // Given a registration with an active general admission ticket
    // When a team member changes the attendee's tickets to a different ticket type
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task ChangeAttendeeTickets_Organizer_Returns200()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { TicketTypeIds = new[] { ChangeAttendeeTicketsFixture.WorkshopId.Value } };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Non-member (Bob) attempts to change ticket types — returns 403
    // Given a registration with an active general admission ticket
    // When a user who is not a member of the team tries to change the attendee's tickets
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task ChangeAttendeeTickets_NonMember_Returns403()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { TicketTypeIds = new[] { ChangeAttendeeTicketsFixture.WorkshopId.Value } };

        var response = await Environment.BobApiClient.PutAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
