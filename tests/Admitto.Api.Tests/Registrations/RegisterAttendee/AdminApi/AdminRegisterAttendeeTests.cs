using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.RegisterAttendee.AdminApi;

[TestClass]
public sealed class AdminRegisterAttendeeTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task CrewMember_CannotAddRegistration_Returns403Forbidden()
    {
        var fixture = AdminRegisterAttendeeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Email = "alice@example.com",
            TicketTypeSlugs = new[] { AdminRegisterAttendeeFixture.TicketTypeSlug }
        };

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            AdminRegisterAttendeeFixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task InvalidEmail_Returns400BadRequest()
    {
        var fixture = AdminRegisterAttendeeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Email = "not-an-email",
            TicketTypeSlugs = new[] { AdminRegisterAttendeeFixture.TicketTypeSlug }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            AdminRegisterAttendeeFixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task EmptyTicketTypeSlugs_Returns400BadRequest()
    {
        var fixture = AdminRegisterAttendeeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Email = "alice@example.com",
            TicketTypeSlugs = Array.Empty<string>()
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            AdminRegisterAttendeeFixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Admin_AddsRegistration_Returns201Created()
    {
        var fixture = AdminRegisterAttendeeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            FirstName = "Alice",
            LastName = "Anderson",
            Email = "alice@example.com",
            TicketTypeSlugs = new[] { AdminRegisterAttendeeFixture.TicketTypeSlug }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            AdminRegisterAttendeeFixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AdminRegisterAttendeeResponse>(
            cancellationToken: testContext.CancellationToken);
        body.ShouldNotBeNull();
        body.RegistrationId.ShouldNotBe(Guid.Empty);
    }

    private sealed record AdminRegisterAttendeeResponse(Guid RegistrationId);
}
