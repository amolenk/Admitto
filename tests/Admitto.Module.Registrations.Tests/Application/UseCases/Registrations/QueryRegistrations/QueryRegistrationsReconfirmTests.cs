using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.QueryRegistrations;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.QueryRegistrations;

/// <summary>
/// Reconfirm-flow filter tests: validates that the
/// <see cref="QueryRegistrationsHandler"/> projection used by the Email
/// module's resolver/scheduler produces the right recipient set across the
/// reconfirm tick lifecycle (per design D5).
/// </summary>
[TestClass]
public sealed class QueryRegistrationsReconfirmTests(TestContext testContext) : AspireIntegrationTestBase
{
    private const string Slug = "general-admission";

    private static readonly QueryRegistrationsDto ReconfirmFilter = new(
        RegistrationStatus: RegistrationStatus.Registered,
        HasReconfirmed: false);

    [TestMethod]
    public async ValueTask SC001_ReconfirmedAttendee_IsExcluded_OnEveryTick()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        await Environment.Database.SeedAsync(db =>
        {
            db.Registrations.Add(NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: true));
            db.Registrations.Add(NewRegistration(teamId, eventId, "bob@example.com", reconfirmed: false));
        });

        // First tick.
        var first = await Query(eventId);
        first.Select(r => r.Email).ShouldBe(["bob@example.com"]);

        // Second tick (no state change): same exclusion.
        var second = await Query(eventId);
        second.Select(r => r.Email).ShouldBe(["bob@example.com"]);
    }

    [TestMethod]
    public async ValueTask SC002_AttendeeWhoReconfirmsBetweenTicks_IsExcludedNextTick()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        Registration alice = null!;

        await Environment.Database.SeedAsync(db =>
        {
            alice = NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: false);
            db.Registrations.Add(alice);
        });

        var beforeReconfirm = await Query(eventId);
        beforeReconfirm.ShouldHaveSingleItem().Email.ShouldBe("alice@example.com");

        // Simulate Alice reconfirming between ticks.
        await Environment.Database.SeedAsync(db =>
        {
            var fromDb = db.Registrations.Find(alice.Id);
            fromDb.ShouldNotBeNull();
            fromDb.Reconfirm(DateTimeOffset.UtcNow);
        });

        var afterReconfirm = await Query(eventId);
        afterReconfirm.ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask SC003_NewRegistrationBetweenTicks_IsPickedUpOnNextTick()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        await Environment.Database.SeedAsync(db =>
            db.Registrations.Add(NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: false)));

        (await Query(eventId)).Select(r => r.Email).ShouldBe(["alice@example.com"]);

        await Environment.Database.SeedAsync(db =>
            db.Registrations.Add(NewRegistration(teamId, eventId, "bob@example.com", reconfirmed: false)));

        var second = await Query(eventId);
        second.Select(r => r.Email).OrderBy(e => e).ShouldBe(["alice@example.com", "bob@example.com"]);
    }

    [TestMethod]
    public async ValueTask SC004_EveryoneReconfirmed_ReturnsEmpty()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        await Environment.Database.SeedAsync(db =>
        {
            db.Registrations.Add(NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: true));
            db.Registrations.Add(NewRegistration(teamId, eventId, "bob@example.com", reconfirmed: true));
        });

        (await Query(eventId)).ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask SC005_CancelledRegistration_IsExcluded_EvenIfNotReconfirmed()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        Registration cancelled = null!;

        await Environment.Database.SeedAsync(db =>
        {
            cancelled = NewRegistration(teamId, eventId, "alice@example.com", reconfirmed: false);
            db.Registrations.Add(cancelled);
        });

        await Environment.Database.SeedAsync(db =>
        {
            var fromDb = db.Registrations.Find(cancelled.Id);
            fromDb.ShouldNotBeNull();
            fromDb.Cancel(CancellationReason.AttendeeRequest);
        });

        (await Query(eventId)).ShouldBeEmpty();
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
            [new TicketTypeSnapshot(Slug, [])]);

        if (reconfirmed)
            registration.Reconfirm(DateTimeOffset.UtcNow);

        return registration;
    }

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private async ValueTask<IReadOnlyList<RegistrationListItemDto>> Query(TicketedEventId eventId) =>
        await new QueryRegistrationsHandler(Environment.Database.Context).HandleAsync(
            new QueryRegistrationsQuery(eventId, ReconfirmFilter),
            testContext.CancellationToken);
}
