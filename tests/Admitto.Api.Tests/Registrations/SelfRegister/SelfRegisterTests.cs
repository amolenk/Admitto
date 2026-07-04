using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfRegister;

[TestClass]
public sealed class SelfRegisterTests(TestContext testContext) : EndToEndTestBase
{
    // Successful self-service registration returns 201 Created
    [TestMethod]
    public async Task SelfRegister_ValidToken_Returns201()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        var token = await fixture.GetVerificationTokenAsync(Environment, testContext.CancellationToken);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, fixture.RegisterRoute)
        {
            Content = JsonContent.Create(new
            {
                Email = SelfRegisterFixture.AttendeeEmail,
                FirstName = "Dave",
                LastName = "Smith",
                RegisterTicketTypeIds = new[] { SelfRegisterFixture.TicketTypeId.Value },
                WaitlistTicketTypeIds = Array.Empty<Guid>()
            }),
            Headers = { Authorization = new("Bearer", token) }
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // Token missing returns 401
    [TestMethod]
    public async Task SelfRegister_MissingToken_Returns401()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.RegisterRoute,
            new
            {
                Email = SelfRegisterFixture.AttendeeEmail,
                FirstName = "Dave",
                LastName = "Smith",
                RegisterTicketTypeIds = new[] { SelfRegisterFixture.TicketTypeId.Value },
                WaitlistTicketTypeIds = Array.Empty<Guid>()
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Invalid/tampered token returns 401
    [TestMethod]
    public async Task SelfRegister_InvalidToken_Returns401()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, fixture.RegisterRoute)
        {
            Content = JsonContent.Create(new
            {
                Email = SelfRegisterFixture.AttendeeEmail,
                FirstName = "Dave",
                LastName = "Smith",
                RegisterTicketTypeIds = new[] { SelfRegisterFixture.TicketTypeId.Value },
                WaitlistTicketTypeIds = Array.Empty<Guid>()
            }),
            Headers = { Authorization = new("Bearer", "this.is.not.a.valid.token") }
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("code").GetString().ShouldBe("email.verification_invalid");
        body.TryGetProperty("registerableTicketTypeIds", out _).ShouldBeFalse();
        body.TryGetProperty("waitlistableTicketTypeIds", out _).ShouldBeFalse();
        body.TryGetProperty("unavailableTicketTypeIds", out _).ShouldBeFalse();
        body.TryGetProperty("unknownTicketTypeIds", out _).ShouldBeFalse();
        body.TryGetProperty("invalidForRequestedActionTicketTypeIds", out _).ShouldBeFalse();
    }

    [TestMethod]
    public async Task SelfRegister_TicketBecameWaitlistable_Returns409WithTicketStates()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, waitlistMode: true);

        var token = await fixture.GetVerificationTokenAsync(Environment, testContext.CancellationToken);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, fixture.RegisterRoute)
        {
            Content = JsonContent.Create(new
            {
                Email = SelfRegisterFixture.AttendeeEmail,
                FirstName = "Dave",
                LastName = "Smith",
                RegisterTicketTypeIds = new[] { SelfRegisterFixture.TicketTypeId.Value },
                WaitlistTicketTypeIds = Array.Empty<Guid>()
            }),
            Headers = { Authorization = new("Bearer", token) }
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("code").GetString().ShouldBe("registration.ticket_state_conflict");
        body.GetProperty("waitlistableTicketTypeIds").EnumerateArray()
            .Select(id => id.GetGuid())
            .ShouldBe([SelfRegisterFixture.TicketTypeId.Value]);
        body.GetProperty("registerableTicketTypeIds").EnumerateArray().ToList().ShouldBeEmpty();
        body.GetProperty("unavailableTicketTypeIds").EnumerateArray().ToList().ShouldBeEmpty();
        body.GetProperty("unknownTicketTypeIds").EnumerateArray().ToList().ShouldBeEmpty();
        body.GetProperty("invalidForRequestedActionTicketTypeIds").EnumerateArray().ToList().ShouldBeEmpty();
    }

    // Token bound to different event returns 401 — uses one team with two events
    [TestMethod]
    public async Task SelfRegister_TokenForDifferentEvent_Returns401()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        var tokenForFirstEvent = await fixture.GetVerificationTokenAsync(Environment, testContext.CancellationToken);

        // Seed a second event (same team, different slug) with registration policy open
        var secondEventId = TicketedEventId.New();
        var secondEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            secondEventId,
            fixture.TeamId,
            EventName.From("Other Event"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));
        secondEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30)));
        var secondCatalog = TicketCatalog.Create(secondEventId, fixture.TeamId);
        secondCatalog.AddTicketType(TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), TicketTypeName.From("General Admission"), [], 100);
        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(secondEvent);
            db.TicketCatalogs.Add(secondCatalog);
        });

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/events/{secondEvent.PublicSlug.Value}/registrations")
        {
            Content = JsonContent.Create(new
            {
                Email = SelfRegisterFixture.AttendeeEmail,
                FirstName = "Dave",
                LastName = "Smith",
                RegisterTicketTypeIds = new[] { SelfRegisterFixture.TicketTypeId.Value },
                WaitlistTicketTypeIds = Array.Empty<Guid>()
            }),
            Headers = { Authorization = new("Bearer", tokenForFirstEvent) }
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task SelfRegister_NonSelfServiceTicketType_Returns409()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, selfServiceEnabled: false);

        var token = await fixture.GetVerificationTokenAsync(Environment, testContext.CancellationToken);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, fixture.RegisterRoute)
        {
            Content = JsonContent.Create(new
            {
                Email = SelfRegisterFixture.AttendeeEmail,
                FirstName = "Dave",
                LastName = "Smith",
                RegisterTicketTypeIds = new[] { SelfRegisterFixture.TicketTypeId.Value },
                WaitlistTicketTypeIds = Array.Empty<Guid>()
            }),
            Headers = { Authorization = new("Bearer", token) }
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
