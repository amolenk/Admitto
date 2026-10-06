using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;

internal sealed class ProcessWaitlistNotificationsFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();
    public TimeZoneId TimeZone { get; } = TimeZoneId.From("UTC");

    private ProcessWaitlistNotificationsFixture()
    {
    }

    /// <summary>
    /// One waitlist entry, one free seat — happy path.
    /// </summary>
    public static ProcessWaitlistNotificationsFixture WithOneEntryOneSlot() =>
        new();

    public static ProcessWaitlistNotificationsFixture WithOneEntryAndQuietHours20To08() =>
        new() { QuietHoursStart = new TimeOnly(20, 0) };

    /// <summary>
    /// Two waitlist entries, only one free seat.
    /// </summary>
    public static ProcessWaitlistNotificationsFixture WithTwoEntriesOneSlot() =>
        new();

    /// <summary>
    /// One waitlist entry, two free seats — fewer entries than seats.
    /// </summary>
    public static ProcessWaitlistNotificationsFixture WithOneEntryTwoSlots() =>
        new() { FreeSeats = 2 };

    /// <summary>
    /// One waitlist entry, no free seat — e.g. only a VIP coupon lapsed.
    /// </summary>
    public static ProcessWaitlistNotificationsFixture WithOneEntryNoSlots() =>
        new() { FreeSeats = 0 };

    /// <summary>
    /// Two waitlist entries and one free seat, with a VIP offer outstanding (which takes no public seat).
    /// </summary>
    public static ProcessWaitlistNotificationsFixture WithTwoEntriesOneSlotAndVipOffer() =>
        new() { OutstandingVipOffers = 1 };

    /// <summary>
    /// Seats released after the ticket type sold out, so they are available to the people waiting.
    /// </summary>
    public int FreeSeats { get; private init; } = 1;

    /// <summary>
    /// VIP offers made while sold out; they take no public hold.
    /// </summary>
    public int OutstandingVipOffers { get; private init; }

    private TimeOnly QuietHoursStart { get; init; } = new(22, 0);

    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        int activeEntries = 1,
        int publicCapacity = 2)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            // TicketedEvent — needed for timezone + quiet hours
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("DevConf 2026"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZone);
            ticketedEvent.ConfigureWaitlistPolicy(QuietHoursStart, new TimeOnly(8, 0));
            dbContext.TicketedEvents.Add(ticketedEvent);

            // TicketCatalog — ticket type with WaitlistEnabled + WaitlistMode active
            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(TicketTypeId, TicketTypeName.From("Conference Pass"), [], publicCapacity, waitlistEnabled: true, claimWindowHours: 8);

            // Fill to capacity and trigger WaitlistMode
            var tickets = Enumerable.Range(0, publicCapacity)
                .Select(_ => catalog.Claim([TicketTypeId], ClaimMode.Public))
                .ToList();

            // Waitlist with active entries; VIP offers are made to the back of the queue while sold out
            var waitlist = global::Amolenk.Admitto.Core.Registrations.Domain.Entities.Waitlist.Create(EventId, TicketTypeId, TeamId);
            var now = DateTimeOffset.UtcNow;
            for (var i = 0; i < activeEntries + OutstandingVipOffers; i++)
                waitlist.AddEntry(EmailAddress.From($"attendee{i + 1}@example.com"), now.AddMinutes(i), catalog, RegistrationId.New());
            for (var i = 0; i < OutstandingVipOffers; i++)
            {
                var vipEntry = waitlist.Entries.Where(e => e.Status == WaitlistEntryStatus.Active).MaxBy(e => e.Position)!;
                dbContext.Coupons.Add(waitlist.IssueCouponToEntry(vipEntry.Id, ticketedEvent, catalog, now));
            }

            foreach (var ticket in tickets.Take(FreeSeats))
                catalog.Release(ticket);

            catalog.ClearDomainEvents();
            waitlist.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);
            dbContext.Waitlists.Add(waitlist);
        });
    }
}
