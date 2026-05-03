using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.ChangeAttendeeTickets;

[TestClass]
public sealed class ChangeAttendeeTicketsTests(TestContext testContext) : EndToEndTestBase
{
    // SC013: Authenticated organizer changes ticket types — returns 200 OK
    [TestMethod]
    public async Task SC013_ChangeAttendeeTickets_Organizer_Returns200()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { TicketTypeSlugs = new[] { "workshop" } };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // SC014: Non-member (Bob) attempts to change ticket types — returns 403
    [TestMethod]
    public async Task SC014_ChangeAttendeeTickets_NonMember_Returns403()
    {
        var fixture = ChangeAttendeeTicketsFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { TicketTypeSlugs = new[] { "workshop" } };

        var response = await Environment.BobApiClient.PutAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
