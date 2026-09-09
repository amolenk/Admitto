using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails;

[TestClass]
public sealed class GetTicketedEventDetailsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a ticketed event configured with registration, reconfirm, and waitlist policies
    // When the ticketed event details are queried
    // Then the response includes the event's status and all configured policy values
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
                    TimeSpan.FromHours(24),
                    new TimeOnly(22, 0),
                    new TimeOnly(8, 0)));
            te.ConfigureWaitlistPolicy(new TimeOnly(23, 0), new TimeOnly(7, 0));

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

        // Postgres timestamptz has microsecond precision; DateTimeOffset ticks are
        // 100ns, so a round trip can shave sub-microsecond ticks off the seeded value.
        var precisionTolerance = TimeSpan.FromMilliseconds(1);

        result.RegistrationPolicy.ShouldNotBeNull();
        result.RegistrationPolicy.OpensAt.ShouldBe(opensAt, precisionTolerance);
        result.RegistrationPolicy.ClosesAt.ShouldBe(closesAt, precisionTolerance);
        result.RegistrationPolicy.AllowedEmailDomain.ShouldBe("@example.com");

        result.ReconfirmPolicy.ShouldNotBeNull();
        result.ReconfirmPolicy.OpensAt.ShouldBe(reconfirmOpens, precisionTolerance);
        result.ReconfirmPolicy.ClosesAt.ShouldBe(reconfirmCloses, precisionTolerance);
        result.ReconfirmPolicy.MinEmailIntervalHours.ShouldBe(24);
        result.ReconfirmPolicy.QuietHoursStart.ShouldBe((TimeOnly?)new TimeOnly(22, 0));
        result.ReconfirmPolicy.QuietHoursEnd.ShouldBe((TimeOnly?)new TimeOnly(8, 0));

        result.WaitlistPolicy.QuietHoursStart.ShouldBe(new TimeOnly(23, 0));
        result.WaitlistPolicy.QuietHoursEnd.ShouldBe(new TimeOnly(7, 0));
    }

    // Given a ticketed event with no registration or reconfirm policy configured
    // When the ticketed event details are queried
    // Then the registration and reconfirm policies are null and the waitlist policy uses defaults
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
        result.WaitlistPolicy.QuietHoursStart.ShouldBe(new TimeOnly(22, 0));
        result.WaitlistPolicy.QuietHoursEnd.ShouldBe(new TimeOnly(8, 0));
        result.IsRegistrationOpen.ShouldBeFalse();
    }

    // Given no ticketed event exists for the given ids
    // When the ticketed event details are queried
    // Then the result is null
    [TestMethod]
    public async ValueTask GetTicketedEventDetails_NotFound_ReturnsNull()
    {
        var sut = new GetTicketedEventDetailsHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await sut.HandleAsync(
            new GetTicketedEventDetailsQuery(TicketedEventId.New(), TeamId.New()),
            testContext.CancellationToken);

        result.ShouldBeNull();
    }

    // Given a ticketed event that belongs to a different team
    // When the ticketed event details are queried with the wrong team id
    // Then the result is null
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
