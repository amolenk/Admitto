using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfRegister;

[TestClass]
public sealed class SelfRegisterTests(TestContext testContext) : EndToEndTestBase
{
    // Successful self-service registration returns 201 Created
    [TestMethod]
    public async Task SC001_SelfRegister_ValidToken_Returns201()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        var token = await fixture.GetVerificationTokenAsync(Environment, testContext.CancellationToken);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, fixture.RegisterRoute)
        {
            Content = JsonContent.Create(new
            {
                FirstName = "Dave",
                LastName = "Smith",
                TicketTypeSlugs = new[] { "general-admission" }
            }),
            Headers = { Authorization = new("Bearer", token) }
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // Token missing returns 401
    [TestMethod]
    public async Task SC002_SelfRegister_MissingToken_Returns401()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.RegisterRoute,
            new
            {
                FirstName = "Dave",
                LastName = "Smith",
                TicketTypeSlugs = new[] { "general-admission" }
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Invalid/tampered token returns 401
    [TestMethod]
    public async Task SC003_SelfRegister_InvalidToken_Returns401()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, fixture.RegisterRoute)
        {
            Content = JsonContent.Create(new
            {
                FirstName = "Dave",
                LastName = "Smith",
                TicketTypeSlugs = new[] { "general-admission" }
            }),
            Headers = { Authorization = new("Bearer", "this.is.not.a.valid.token") }
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Token bound to different event returns 401 — uses one team with two events
    [TestMethod]
    public async Task SC004_SelfRegister_TokenForDifferentEvent_Returns401()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        var tokenForFirstEvent = await fixture.GetVerificationTokenAsync(Environment, testContext.CancellationToken);

        // Seed a second event (same team, different slug) with registration policy open
        var secondEventId = TicketedEventId.New();
        var secondEvent = TicketedEvent.Create(
            secondEventId,
            fixture.TeamId,
            Slug.From(SelfRegisterFixture.TeamSlug),
            Slug.From("other-event"),
            DisplayName.From("Other Event"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));
        secondEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30)));
        var secondCatalog = TicketCatalog.Create(secondEventId);
        secondCatalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(secondEvent);
            db.TicketCatalogs.Add(secondCatalog);
        });

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/teams/{SelfRegisterFixture.TeamSlug}/events/other-event/registrations")
        {
            Content = JsonContent.Create(new
            {
                FirstName = "Dave",
                LastName = "Smith",
                TicketTypeSlugs = new[] { "general-admission" }
            }),
            Headers = { Authorization = new("Bearer", tokenForFirstEvent) }
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // SC: Attempting to self-register with an admin-only (non-self-service) ticket type returns 400
    [TestMethod]
    public async Task SC010_SelfRegister_NonSelfServiceTicketType_Returns400()
    {
        var fixture = SelfRegisterFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, selfServiceEnabled: false);

        var token = await fixture.GetVerificationTokenAsync(Environment, testContext.CancellationToken);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, fixture.RegisterRoute)
        {
            Content = JsonContent.Create(new
            {
                FirstName = "Dave",
                LastName = "Smith",
                TicketTypeSlugs = new[] { "general-admission" }
            }),
            Headers = { Authorization = new("Bearer", token) }
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
