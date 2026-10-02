using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;

/// <summary>
/// A sold-out, waitlist-enabled ticket type in WaitlistMode with people waiting, for the capacity-change and
/// disable flows of <c>UpdateTicketType</c>.
/// </summary>
internal sealed class UpdateTicketTypeWaitlistFixture
{
    private int _waitingCount;
    private int _vipOffers;
    private bool _withOutstandingCoupon;
    private bool _withWaitlistedRegistration;
    private bool _withMixedRegistration;

    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();
    public TicketTypeId WorkshopTicketTypeId { get; } = TicketTypeId.New();
    public TicketTypeId DinnerTicketTypeId { get; } = TicketTypeId.New();
    public const int PublicCapacity = 1;
    public static EmailAddress OfferedEmail { get; } = EmailAddress.From("offered@example.com");
    public Guid OutstandingCouponCode { get; private set; }
    public RegistrationId WaitlistedRegistrationId { get; private set; }
    public RegistrationId MixedRegistrationId { get; private set; }

    public static EmailAddress WaitingEmail(int position) => EmailAddress.From($"attendee{position}@example.com");

    public static EmailAddress VipEmail(int index) => EmailAddress.From($"vip{index}@example.com");

    public static IReadOnlyList<EmailAddress> VipEmails { get; } = Enumerable.Range(1, 5).Select(VipEmail).ToList();

    private UpdateTicketTypeWaitlistFixture()
    {
    }

    /// <summary>
    /// The given number of attendees waiting, in queue order <c>attendee1..N@example.com</c>.
    /// </summary>
    public static UpdateTicketTypeWaitlistFixture WithWaitingEntries(int count) =>
        new() { _waitingCount = count };

    /// <summary>
    /// Attendees waiting, plus an offer already sent to <see cref="OfferedEmail"/> that has not been claimed yet.
    /// </summary>
    public static UpdateTicketTypeWaitlistFixture WithWaitingEntriesAndOutstandingCoupon(int count) =>
        new() { _waitingCount = count, _withOutstandingCoupon = true };

    /// <summary>
    /// Attendees waiting, plus <paramref name="vipOffers"/> VIP offers (to <see cref="VipEmail"/>) made while the ticket
    /// type was sold out. VIP offers take no public hold.
    /// </summary>
    public static UpdateTicketTypeWaitlistFixture WithWaitingEntriesAndVipOffers(int waitingCount, int vipOffers) =>
        new() { _waitingCount = waitingCount, _vipOffers = vipOffers };

    /// <summary>
    /// The first waiting attendee holds a <see cref="RegistrationStatus.Waitlisted"/> registration (no confirmed tickets).
    /// </summary>
    public static UpdateTicketTypeWaitlistFixture WithWaitlistedRegistration() =>
        new() { _waitingCount = 2, _withWaitlistedRegistration = true };

    /// <summary>
    /// The first waiting attendee holds a <see cref="RegistrationStatus.Registered"/> registration with a confirmed
    /// Workshop ticket while waiting for the Conference Pass. A Dinner ticket type with free capacity is also on sale.
    /// </summary>
    public static UpdateTicketTypeWaitlistFixture WithMixedRegistration() =>
        new() { _waitingCount = 2, _withMixedRegistration = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("DevConf"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));
            ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(20)));
            ticketedEvent.ClearDomainEvents();
            dbContext.TicketedEvents.Add(ticketedEvent);

            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(
                TicketTypeId, TicketTypeName.From("Conference Pass"), [], PublicCapacity, waitlistEnabled: true);
            for (var i = 0; i < PublicCapacity; i++)
                catalog.Claim([TicketTypeId], ClaimMode.Public); // sells out → WaitlistMode
            catalog.AddTicketType(WorkshopTicketTypeId, TicketTypeName.From("Workshop"), [], 10);
            catalog.AddTicketType(DinnerTicketTypeId, TicketTypeName.From("Dinner"), [], 10);
            var workshopTickets = _withMixedRegistration
                ? catalog.Claim([WorkshopTicketTypeId], ClaimMode.Public)
                : [];
            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);

            var waitlist = Waitlist.Create(EventId, TicketTypeId, TeamId);
            var now = DateTimeOffset.UtcNow;

            if (_withOutstandingCoupon)
            {
                waitlist.AddEntry(OfferedEmail, now.AddMinutes(-1), catalog, RegistrationId.New());
                var coupon = waitlist.IssueNextCoupon(ticketedEvent, catalog, now)!;
                coupon.ClearDomainEvents();
                OutstandingCouponCode = coupon.Code.Value;
                dbContext.Coupons.Add(coupon);
            }

            for (var i = 1; i <= _waitingCount; i++)
                waitlist.AddEntry(WaitingEmail(i), now.AddMinutes(i), catalog, RegistrationId.New());

            for (var i = 1; i <= _vipOffers; i++)
            {
                waitlist.AddEntry(VipEmail(i), now.AddMinutes(_waitingCount + i), catalog, RegistrationId.New());
                var vipEntry = waitlist.Entries.Single(e => e.Email == VipEmail(i) && e.Status == WaitlistEntryStatus.Active);
                var vipCoupon = waitlist.IssueCouponToEntry(vipEntry.Id, ticketedEvent, catalog, now);
                vipCoupon.ClearDomainEvents();
                dbContext.Coupons.Add(vipCoupon);
            }

            waitlist.ClearDomainEvents();
            dbContext.Waitlists.Add(waitlist);

            if (_withWaitlistedRegistration)
            {
                var registration = Registration.Create(
                    TeamId,
                    EventId,
                    WaitingEmail(1),
                    FirstName.From("Alice"),
                    LastName.From("Doe"),
                    [],
                    waitlistedTickets: [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("Conference Pass"), [])]);
                registration.ClearDomainEvents();
                WaitlistedRegistrationId = registration.Id;
                dbContext.Registrations.Add(registration);
            }

            if (_withMixedRegistration)
            {
                var registration = Registration.Create(
                    TeamId,
                    EventId,
                    WaitingEmail(1),
                    FirstName.From("Alice"),
                    LastName.From("Doe"),
                    workshopTickets,
                    waitlistedTickets: [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("Conference Pass"), [])]);
                registration.ClearDomainEvents();
                MixedRegistrationId = registration.Id;
                dbContext.Registrations.Add(registration);
            }
        });
    }
}
