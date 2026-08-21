using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using GetRegistrationsNs = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.GetRegistrations;

/// <summary>
/// Reconfirm-flow filter tests: validates that the
/// <see cref="GetRegistrationsHandler"/> projection used by the Email
/// module's resolver/scheduler produces the right recipient set across the
/// reconfirm tick lifecycle.
/// </summary>
[TestClass]
public sealed class GetRegistrationsReconfirmTests(TestContext testContext) : AspireIntegrationTestBase
{

    private static readonly QueryRegistrationsDto ReconfirmFilter = new(
        RegistrationStatus: RegistrationStatus.Registered,
        HasReconfirmed: false);

    [TestMethod]
    public async ValueTask HandleAsync_ReconfirmedAttendee_ExcludedOnEveryTick()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(NewTicketedEvent(teamId, eventId));
            db.Registrations.Add(NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: true));
            db.Registrations.Add(NewRegistration(teamId, eventId, "bob@example.com", reconfirmed: false));
        });

        // First tick.
        var first = await Query(teamId, eventId);
        first.Select(r => r.Email).ShouldBe(["bob@example.com"]);

        // Second tick (no state change): same exclusion.
        var second = await Query(teamId, eventId);
        second.Select(r => r.Email).ShouldBe(["bob@example.com"]);
    }

    [TestMethod]
    public async ValueTask HandleAsync_AttendeeReconfirmsBetweenTicks_ExcludedOnNextTick()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        Registration alice = null!;

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(NewTicketedEvent(teamId, eventId));
            alice = NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: false);
            db.Registrations.Add(alice);
        });

        var beforeReconfirm = await Query(teamId, eventId);
        beforeReconfirm.ShouldHaveSingleItem().Email.ShouldBe("alice@example.com");

        // Simulate Alice reconfirming between ticks.
        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            var fromDb = db.Registrations.Find(alice.Id);
            fromDb.ShouldNotBeNull();
            fromDb.Reconfirm(DateTimeOffset.UtcNow);
        });

        var afterReconfirm = await Query(teamId, eventId);
        afterReconfirm.ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask HandleAsync_NewRegistrationBetweenTicks_IncludedOnNextTick()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(NewTicketedEvent(teamId, eventId));
            db.Registrations.Add(NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: false));
        });

        (await Query(teamId, eventId)).Select(r => r.Email).ShouldBe(["alice@example.com"]);

        await Environment.RegistrationsDatabase.SeedAsync(db =>
            db.Registrations.Add(NewRegistration(teamId, eventId, "bob@example.com", reconfirmed: false)));

        var second = await Query(teamId, eventId);
        second.Select(r => r.Email).OrderBy(e => e).ShouldBe(["alice@example.com", "bob@example.com"]);
    }

    [TestMethod]
    public async ValueTask HandleAsync_EveryoneReconfirmed_ReturnsEmpty()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(NewTicketedEvent(teamId, eventId));
            db.Registrations.Add(NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: true));
            db.Registrations.Add(NewRegistration(teamId, eventId, "bob@example.com", reconfirmed: true));
        });

        (await Query(teamId, eventId)).ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask HandleAsync_CancelledRegistration_ExcludedEvenIfNotReconfirmed()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        Registration cancelled = null!;

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(NewTicketedEvent(teamId, eventId));
            cancelled = NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: false);
            db.Registrations.Add(cancelled);
        });

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            var fromDb = db.Registrations.Find(cancelled.Id);
            fromDb.ShouldNotBeNull();
            fromDb.Cancel(CancellationReason.AttendeeRequest);
        });

        (await Query(teamId, eventId)).ShouldBeEmpty();
    }

    private static Registration NewRegistration(
        TeamId teamId, TicketedEventId eventId, string email, bool reconfirmed)
    {
        var emailParts = email.Split('@')[0].Split('.');
        var registration = Registration.Create(
            teamId,
            eventId,
            EmailAddress.From(email),
            FirstName.From(Capitalize(emailParts[0])),
            LastName.From("Doe"),
            [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("General Admission"), [])]);

        if (reconfirmed)
            registration.Reconfirm(DateTimeOffset.UtcNow);

        return registration;
    }

    private static TicketedEvent NewTicketedEvent(TeamId teamId, TicketedEventId eventId) =>
        TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            teamId,
            EventName.From("Test Event"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            TimeZoneId.From("UTC"));

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private async ValueTask<IReadOnlyList<GetRegistrationsNs.RegistrationListItemDto>> Query(TeamId teamId, TicketedEventId eventId) =>
        (await new GetRegistrationsNs.GetRegistrationsHandler(Environment.RegistrationsDatabase.Context).HandleAsync(
            new GetRegistrationsNs.GetRegistrationsQuery(eventId, teamId, ReconfirmFilter),
            testContext.CancellationToken)) ?? [];
}
