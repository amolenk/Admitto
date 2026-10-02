using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries;

internal sealed class WithdrawWaitlistEntriesFixture
{
    private enum Scenario
    {
        TwoTicketTypes,
        SurroundingActiveEntries,
        NoWaitlists,
        OutstandingOffer
    }

    private Scenario _scenario;

    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public EmailAddress CancelledAttendeeEmail { get; } = EmailAddress.From("alice@example.com");
    public List<TicketTypeId> TicketTypeIds { get; } = [];
    public EmailAddress OtherAttendeeBeforeEmail { get; } = EmailAddress.From("before@example.com");
    public EmailAddress OtherAttendeeAfterEmail { get; } = EmailAddress.From("after@example.com");
    public EmailAddress NextQueuedEmail { get; } = EmailAddress.From("next@example.com");
    public CouponId OfferedCouponId { get; private set; }

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

    /// <summary>
    /// A sold-out, waitlist-mode ticket type where the cancelled attendee holds an outstanding automatic offer
    /// (its hold hasn't been redeemed yet), with another attendee still queued behind them.
    /// </summary>
    public static WithdrawWaitlistEntriesFixture WithOutstandingOffer() =>
        new() { _scenario = Scenario.OutstandingOffer };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var now = DateTimeOffset.UtcNow;
            var catalog = TicketCatalog.Create(EventId, TeamId);
            dbContext.TicketCatalogs.Add(catalog);

            switch (_scenario)
            {
                case Scenario.TwoTicketTypes:
                    for (var i = 0; i < 2; i++)
                    {
                        var ticketTypeId = TicketTypeId.New();
                        TicketTypeIds.Add(ticketTypeId);
                        AddWaitlistEnabledTicketType(catalog, ticketTypeId, $"Workshop {i + 1}");

                        var waitlist = Waitlist.Create(EventId, ticketTypeId, TeamId);
                        waitlist.AddEntry(CancelledAttendeeEmail, now, catalog, RegistrationId.New());
                        dbContext.Waitlists.Add(waitlist);
                    }
                    break;

                case Scenario.SurroundingActiveEntries:
                {
                    var ticketTypeId = TicketTypeId.New();
                    TicketTypeIds.Add(ticketTypeId);
                    AddWaitlistEnabledTicketType(catalog, ticketTypeId, "Workshop");

                    var waitlist = Waitlist.Create(EventId, ticketTypeId, TeamId);
                    waitlist.AddEntry(OtherAttendeeBeforeEmail, now, catalog, RegistrationId.New()); // position 1
                    waitlist.AddEntry(CancelledAttendeeEmail, now.AddMinutes(1), catalog, RegistrationId.New()); // position 2 - withdrawn
                    waitlist.AddEntry(OtherAttendeeAfterEmail, now.AddMinutes(2), catalog, RegistrationId.New()); // position 3 -> becomes 2
                    dbContext.Waitlists.Add(waitlist);
                    break;
                }

                case Scenario.OutstandingOffer:
                {
                    var ticketTypeId = TicketTypeId.New();
                    TicketTypeIds.Add(ticketTypeId);
                    catalog.AddTicketType(ticketTypeId, TicketTypeName.From("Workshop"), [], publicCapacity: 1, waitlistEnabled: true);
                    catalog.Claim([ticketTypeId], ClaimMode.Public);

                    var waitlist = Waitlist.Create(EventId, ticketTypeId, TeamId);
                    waitlist.AddEntry(CancelledAttendeeEmail, now, catalog, RegistrationId.New());
                    waitlist.AddEntry(NextQueuedEmail, now.AddMinutes(1), catalog, RegistrationId.New());

                    var ticketedEvent = TicketedEvent.Create(
                        CreationRequestId.From(Guid.NewGuid()),
                        EventId, TeamId,
                        EventName.From("DevConf"),
                        AbsoluteUrl.From("https://example.com"),
                        AbsoluteUrl.From("https://tickets.example.com"),
                        now.AddDays(10), now.AddDays(11),
                        TimeZoneId.From("UTC"));
                    var coupon = waitlist.IssueNextCoupon(ticketedEvent, catalog, now)!;
                    OfferedCouponId = coupon.Id;
                    dbContext.Waitlists.Add(waitlist);
                    dbContext.Coupons.Add(coupon);
                    break;
                }

                case Scenario.NoWaitlists:
                    break;
            }
        });
    }

    private static void AddWaitlistEnabledTicketType(TicketCatalog catalog, TicketTypeId id, string name) =>
        catalog.AddTicketType(id, TicketTypeName.From(name), [], publicCapacity: 10, waitlistEnabled: true);
}
