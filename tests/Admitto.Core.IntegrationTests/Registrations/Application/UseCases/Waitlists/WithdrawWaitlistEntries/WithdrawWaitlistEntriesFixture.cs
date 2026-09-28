using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries;

internal sealed class WithdrawWaitlistEntriesFixture
{
    private enum Scenario
    {
        TwoTicketTypes,
        SurroundingActiveEntries,
        NoWaitlists
    }

    private Scenario _scenario;

    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public EmailAddress CancelledAttendeeEmail { get; } = EmailAddress.From("alice@example.com");
    public List<TicketTypeId> TicketTypeIds { get; } = [];
    public EmailAddress OtherAttendeeBeforeEmail { get; } = EmailAddress.From("before@example.com");
    public EmailAddress OtherAttendeeAfterEmail { get; } = EmailAddress.From("after@example.com");

    private WithdrawWaitlistEntriesFixture()
    {
    }

    /// <summary>
    /// Two separate waitlists (different ticket types) each with an active entry for the
    /// cancelled attendee's email.
    /// </summary>
    public static WithdrawWaitlistEntriesFixture WithEntriesOnTwoTicketTypes() =>
        new() { _scenario = Scenario.TwoTicketTypes };

    /// <summary>
    /// A single waitlist with the cancelled attendee's entry sandwiched between two other
    /// active entries, to verify renumbering.
    /// </summary>
    public static WithdrawWaitlistEntriesFixture WithSurroundingActiveEntries() =>
        new() { _scenario = Scenario.SurroundingActiveEntries };

    /// <summary>
    /// An event with no waitlists at all.
    /// </summary>
    public static WithdrawWaitlistEntriesFixture WithNoWaitlists() =>
        new() { _scenario = Scenario.NoWaitlists };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var now = DateTimeOffset.UtcNow;

            switch (_scenario)
            {
                case Scenario.TwoTicketTypes:
                    for (var i = 0; i < 2; i++)
                    {
                        var ticketTypeId = TicketTypeId.New();
                        TicketTypeIds.Add(ticketTypeId);

                        var waitlist = Waitlist.Create(EventId, ticketTypeId, TeamId);
                        waitlist.AddEntry(CancelledAttendeeEmail, now);
                        dbContext.Waitlists.Add(waitlist);
                    }
                    break;

                case Scenario.SurroundingActiveEntries:
                {
                    var ticketTypeId = TicketTypeId.New();
                    TicketTypeIds.Add(ticketTypeId);

                    var waitlist = Waitlist.Create(EventId, ticketTypeId, TeamId);
                    waitlist.AddEntry(OtherAttendeeBeforeEmail, now); // position 1
                    waitlist.AddEntry(CancelledAttendeeEmail, now.AddMinutes(1)); // position 2 - withdrawn
                    waitlist.AddEntry(OtherAttendeeAfterEmail, now.AddMinutes(2)); // position 3 -> becomes 2
                    dbContext.Waitlists.Add(waitlist);
                    break;
                }

                case Scenario.NoWaitlists:
                    break;
            }
        });
    }
}
