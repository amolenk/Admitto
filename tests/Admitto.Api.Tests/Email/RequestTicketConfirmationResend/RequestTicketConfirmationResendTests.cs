using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Email.RequestTicketConfirmationResend;

[TestClass]
public sealed class RequestTicketConfirmationResendTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task Anonymous_Returns401()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.PostAsync(
            fixture.ResendRoute,
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task NonMember_Returns403()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.PostAsync(
            fixture.ResendRoute,
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task MissingRegistration_Returns404()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsync(
            $"/admin/teams/{fixture.TeamId}/events/{fixture.EventId}/registrations/{Guid.NewGuid()}/ticket-email/resend",
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task CancelledRegistration_ReturnsConflictProblem()
    {
        var fixture = RequestTicketConfirmationResendFixture.CancelledAttendee();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsync(
            fixture.ResendRoute,
            content: null,
            testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        body.GetProperty("code").GetString().ShouldBe("registration.not_registered");
    }

    [TestMethod]
    public async Task RegisteredRegistration_ReturnsAcceptedAndCreatesDurableResendRequest()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsync(
            fixture.ResendRoute,
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        response.Headers.Location.ShouldBeNull();

        Environment.RegistrationsDatabase.Context.ChangeTracker.Clear();
        var outboxMessage = await Environment.RegistrationsDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(
                m => m.Type == "Registrations:TicketConfirmationResendRequestedIntegrationEvent",
                testContext.CancellationToken);

        outboxMessage.Payload.RootElement.GetProperty("registrationId").GetGuid()
            .ShouldBe(fixture.RegistrationId.Value);
    }

    [TestMethod]
    public async Task PartnerRequestTicketConfirmationResend_RegisteredRegistration_ReturnsAcceptedAndCreatesDurableResendRequest()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendee();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(
            fixture.PartnerResendRoute,
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldBeNull();

        var outboxMessage = await Environment.RegistrationsDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(
                m => m.Type == "Registrations:TicketConfirmationResendRequestedIntegrationEvent",
                testContext.CancellationToken);

        outboxMessage.Payload.RootElement.GetProperty("registrationId").GetGuid()
            .ShouldBe(fixture.RegistrationId.Value);
    }

    [TestMethod]
    public async Task PartnerRequestTicketConfirmationResend_MissingApiKey_Returns401AndCreatesNoResendRequest()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.PostAsync(
            fixture.PartnerResendRoute,
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await AssertNoResendRequestAsync();
    }

    [TestMethod]
    public async Task PartnerRequestTicketConfirmationResend_InvalidApiKey_Returns401AndCreatesNoResendRequest()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendee();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient("bogus-key-that-does-not-exist");
        var response = await client.PostAsync(
            fixture.PartnerResendRoute,
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await AssertNoResendRequestAsync();
    }

    [TestMethod]
    public async Task PartnerRequestTicketConfirmationResend_RevokedApiKey_Returns401AndCreatesNoResendRequest()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendeeWithRevokedApiKey();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(
            fixture.PartnerResendRoute,
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await AssertNoResendRequestAsync();
    }

    [TestMethod]
    public async Task PartnerRequestTicketConfirmationResend_ApiKeyForOtherTeam_Returns404AndCreatesNoResendRequest()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendeeWithOtherTeamApiKey();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.OtherTeamApiKey);
        var response = await client.PostAsync(
            fixture.PartnerResendRoute,
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await AssertNoResendRequestAsync();
    }

    [TestMethod]
    public async Task PartnerRequestTicketConfirmationResend_MissingRegistration_Returns404AndCreatesNoResendRequest()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var route = $"/api/events/{fixture.EventSlug}/registrations/{Guid.NewGuid()}/ticket-email/resend";
        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(
            route,
            content: null,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await AssertNoResendRequestAsync();
    }

    [TestMethod]
    public async Task PartnerRequestTicketConfirmationResend_CancelledRegistration_ReturnsConflictProblemAndCreatesNoResendRequest()
    {
        var fixture = RequestTicketConfirmationResendFixture.CancelledAttendee();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsync(
            fixture.PartnerResendRoute,
            content: null,
            testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        body.GetProperty("code").GetString().ShouldBe("registration.not_registered");
        await AssertNoResendRequestAsync();
    }

    private async Task AssertNoResendRequestAsync()
    {
        var count = await Environment.RegistrationsDatabase.Context.OutboxMessages
            .AsNoTracking()
            .CountAsync(
                m => m.Type == "Registrations:TicketConfirmationResendRequestedIntegrationEvent",
                testContext.CancellationToken);

        count.ShouldBe(0);
    }
}
