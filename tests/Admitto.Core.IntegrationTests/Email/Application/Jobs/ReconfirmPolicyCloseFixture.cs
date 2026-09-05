using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Email.Application;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs;

internal sealed class ReconfirmPolicyCloseFixture
{
    private readonly IReadOnlyList<RegistrationListItemDto> _candidates;
    private readonly IReadOnlyList<(Guid RegistrationId, string Email, Guid CycleId)> _sentLogs;

    private ReconfirmPolicyCloseFixture(
        DateTimeOffset now,
        IReadOnlyList<RegistrationListItemDto> candidates,
        IReadOnlyList<(Guid RegistrationId, string Email, Guid CycleId)> sentLogs)
    {
        Now = now;
        TeamId = TeamId.New();
        EventId = TicketedEventId.New();
        _candidates = candidates;
        _sentLogs = sentLogs;
    }

    public TeamId TeamId { get; }
    public TicketedEventId EventId { get; }
    public DateTimeOffset Now { get; }
    public Guid MaxedRegistrationId => _candidates[0].RegistrationId;

    public static ReconfirmPolicyCloseFixture MaxedAndBelowMaximum(DateTimeOffset now)
    {
        var maxedId = Guid.NewGuid();
        var belowMaxId = Guid.NewGuid();
        var maxedCycle = Guid.NewGuid();
        var belowMaxCycle = Guid.NewGuid();
        return new(
            now,
            [
                RegistrationItem(maxedId, "maxed@example.com", now.AddDays(-10), 1, maxedCycle),
                RegistrationItem(belowMaxId, "below-max@example.com", now.AddDays(-10), 2, belowMaxCycle)
            ],
            [
                (maxedId, "maxed@example.com", maxedCycle),
                (belowMaxId, "below-max@example.com", belowMaxCycle)
            ]);
    }

    public static ReconfirmPolicyCloseFixture SingleMaxed(DateTimeOffset now)
    {
        var registrationId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        return new(
            now,
            [RegistrationItem(registrationId, "alice@example.com", now.AddDays(-2), 1, cycleId)],
            [(registrationId, "alice@example.com", cycleId)]);
    }

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.EmailDatabase.SeedAsync(db =>
        {
            db.EventEmailContexts.Add(
                new EventEmailContextViewBuilder()
                    .ForTeam(TeamId)
                    .ForEvent(EventId)
                    .At(Now)
                    .WithWindow(Now.AddHours(-1), Now)
                    .WithQuietHours(new TimeOnly(22), new TimeOnly(8))
                    .Build());

            foreach (var (registrationId, email, cycleId) in _sentLogs)
            {
                db.EmailLog.Add(EmailLog.Create(
                    TeamId,
                    EventId,
                    $"reconfirm:{Guid.NewGuid():N}",
                    EmailAddress.From(email),
                    BuiltInEmailTemplateNames.Reconfirmation,
                    "Please reconfirm",
                    EmailLogStatus.Sent,
                    Now.AddMinutes(-30),
                    Now.AddMinutes(-30),
                    registrationId: RegistrationId.From(registrationId),
                    registrationCycleId: RegistrationCycleId.From(cycleId)));
            }
        });
    }

    public IRegistrationsFacade Facade()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                TeamId.Value,
                EventId.Value,
                Arg.Is<QueryRegistrationsDto>(q => MatchesReconfirmQuery(q)),
                Arg.Any<CancellationToken>())
            .Returns(_candidates);
        return facade;
    }

    private static RegistrationListItemDto RegistrationItem(
        Guid registrationId,
        string email,
        DateTimeOffset createdAt,
        int effectiveMaxReconfirmationEmails,
        Guid cycleId) =>
        new(
            registrationId,
            email,
            "Alice",
            "Test",
            [],
            new Dictionary<string, string>(),
            createdAt,
            cycleId,
            1,
            1,
            RegistrationStatus.Registered,
            false,
            null,
            effectiveMaxReconfirmationEmails);

    private static bool MatchesReconfirmQuery(QueryRegistrationsDto? query) =>
        query is not null
        && query.RegistrationStatus == RegistrationStatus.Registered
        && query.HasReconfirmed == false;
}
