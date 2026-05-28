using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails;

[TestClass]
public sealed class GetTicketedEventDetailsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask GetTicketedEventDetails_IncludesAllPolicies()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        var opensAt = DateTimeOffset.UtcNow.AddDays(1);
        var closesAt = DateTimeOffset.UtcNow.AddDays(10);
        var reconfirmOpens = DateTimeOffset.UtcNow.AddDays(11);
        var reconfirmCloses = DateTimeOffset.UtcNow.AddDays(25);

        await Environment.RegistrationsDatabase.SeedAsync(ctx =>
        {
            var te = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                eventId,
                teamId,
                EventName.From("Conf 2026"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));

            te.ConfigureRegistrationPolicy(
                TicketedEventRegistrationPolicy.Create(opensAt, closesAt, "@example.com"));
            te.ConfigureReconfirmPolicy(
                TicketedEventReconfirmPolicy.Create(
                    reconfirmOpens,
                    reconfirmCloses,
                    TimeSpan.FromDays(7),
                    TimeSpan.FromHours(24)));

            ctx.TicketedEvents.Add(te);
        });

        var sut = new GetTicketedEventDetailsHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await sut.HandleAsync(
            new GetTicketedEventDetailsQuery(eventId, teamId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(eventId.Value);
        result.TeamId.ShouldBe(teamId.Value);
        result.Status.ShouldBe(EventLifecycleStatus.Active);

        result.RegistrationPolicy.ShouldNotBeNull();
        result.RegistrationPolicy.OpensAt.ShouldBe(opensAt);
        result.RegistrationPolicy.ClosesAt.ShouldBe(closesAt);
        result.RegistrationPolicy.AllowedEmailDomain.ShouldBe("@example.com");

        result.ReconfirmPolicy.ShouldNotBeNull();
        result.ReconfirmPolicy.OpensAt.ShouldBe(reconfirmOpens);
        result.ReconfirmPolicy.ClosesAt.ShouldBe(reconfirmCloses);
        result.ReconfirmPolicy.CadenceHours.ShouldBe(168);
        result.ReconfirmPolicy.MinEmailIntervalHours.ShouldBe(24);
    }

    [TestMethod]
    public async ValueTask GetTicketedEventDetails_WithoutPolicies_ReturnsNullPolicies()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        await Environment.RegistrationsDatabase.SeedAsync(ctx =>
        {
            var te = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                eventId,
                teamId,
                EventName.From("Bare Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));
            ctx.TicketedEvents.Add(te);
        });

        var sut = new GetTicketedEventDetailsHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await sut.HandleAsync(
            new GetTicketedEventDetailsQuery(eventId, teamId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.RegistrationPolicy.ShouldBeNull();
        result.ReconfirmPolicy.ShouldBeNull();
        result.IsRegistrationOpen.ShouldBeFalse();
    }

    [TestMethod]
    public async ValueTask GetTicketedEventDetails_NotFound_ReturnsNull()
    {
        var sut = new GetTicketedEventDetailsHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await sut.HandleAsync(
            new GetTicketedEventDetailsQuery(TicketedEventId.New(), TeamId.New()),
            testContext.CancellationToken);

        result.ShouldBeNull();
    }

    [TestMethod]
    public async ValueTask GetTicketedEventDetails_WrongTeamId_ReturnsNull()
    {
        // Arrange: create event for team A
        var eventId = TicketedEventId.New();
        var teamIdA = TeamId.New();
        var teamIdB = TeamId.New();

        await Environment.RegistrationsDatabase.SeedAsync(ctx =>
        {
            var te = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                eventId,
                teamIdA,
                EventName.From("Team A Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));
            ctx.TicketedEvents.Add(te);
        });

        var sut = new GetTicketedEventDetailsHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        // Act: query with team B's ID
        var result = await sut.HandleAsync(
            new GetTicketedEventDetailsQuery(eventId, teamIdB),
            testContext.CancellationToken);

        // Assert: cross-team access is rejected
        result.ShouldBeNull();
    }
}
