using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.RegisterAttendee.AdminApi;

[TestClass]
public sealed class AdminRegisterAttendeeTests(TestContext testContext) : EndToEndTestBase
{
    // Given a ticketed event open for registration with an available ticket type
    // When a user who is not a member of the team tries to register an attendee
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task CrewMember_CannotAddRegistration_Returns403Forbidden()
    {
        var fixture = AdminRegisterAttendeeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Email = "alice@example.com",
            TicketTypeIds = new[] { AdminRegisterAttendeeFixture.TicketTypeId.Value }
        };

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given a ticketed event open for registration with an available ticket type
    // When an admin registers an attendee with an invalid email address
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task InvalidEmail_Returns400BadRequest()
    {
        var fixture = AdminRegisterAttendeeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Email = "not-an-email",
            TicketTypeIds = new[] { AdminRegisterAttendeeFixture.TicketTypeId.Value }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given a ticketed event open for registration with an available ticket type
    // When an admin registers an attendee with no ticket types selected
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task EmptyTicketTypeIds_Returns400BadRequest()
    {
        var fixture = AdminRegisterAttendeeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Email = "alice@example.com",
            TicketTypeIds = Array.Empty<Guid>()
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given a ticketed event open for registration with an available ticket type
    // When an admin registers a new attendee with valid details
    // Then the API returns 201 Created with a non-empty registration id
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
            TicketTypeIds = new[] { AdminRegisterAttendeeFixture.TicketTypeId.Value }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AdminRegisterAttendeeResponse>(
            cancellationToken: testContext.CancellationToken);
        body.ShouldNotBeNull();
        body.RegistrationId.ShouldNotBe(Guid.Empty);
    }

    private sealed record AdminRegisterAttendeeResponse(Guid RegistrationId);
}
