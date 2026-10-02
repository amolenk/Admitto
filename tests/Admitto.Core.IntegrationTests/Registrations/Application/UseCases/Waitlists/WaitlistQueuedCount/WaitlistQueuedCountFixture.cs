using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Registrations.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistQueuedCount;

/// <summary>
/// A sold-out, waitlist-enabled ticket type in WaitlistMode: <see cref="PublicCapacity"/> seats taken by registrations
/// of <see cref="RegisteredEmail"/>, and attendees waiting in queue order <see cref="WaitingEmail"/>, each with a
/// <c>Waitlisted</c> registration.
/// </summary>
internal sealed class WaitlistQueuedCountFixture
{
    private readonly List<RegistrationId> _registeredIds = [];
    private readonly List<RegistrationId> _waitingIds = [];
    private int _waitingCount;
    private bool _withOrganiserCouponForFirstWaiting;

    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();
    public const int PublicCapacity = 2;
    public Guid OrganiserCouponCode { get; private set; }

    /// <summary>Registrations holding the sold-out seats.</summary>
    public IReadOnlyList<RegistrationId> RegisteredIds => _registeredIds;

    /// <summary>The waiting attendees' <c>Waitlisted</c> registrations, in queue order.</summary>
    public IReadOnlyList<RegistrationId> WaitingIds => _waitingIds;

    public static EmailAddress RegisteredEmail(int index) => EmailAddress.From($"registered{index}@example.com");

    public static EmailAddress WaitingEmail(int position) => EmailAddress.From($"attendee{position}@example.com");

    private WaitlistQueuedCountFixture()
    {
    }

    /// <summary>
    /// Sold out, with the given number of attendees waiting (zero leaves the queue empty).
    /// </summary>
    public static WaitlistQueuedCountFixture SoldOutWithWaitingEntries(int count) =>
        new() { _waitingCount = count };

    /// <summary>
    /// Sold out, with attendees waiting, and an organiser coupon for the attendee at position 1.
    /// </summary>
    public static WaitlistQueuedCountFixture SoldOutWithWaitingEntriesAndOrganiserCouponForFirst(int count) =>
        new() { _waitingCount = count, _withOrganiserCouponForFirstWaiting = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var now = DateTimeOffset.UtcNow;
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("DevConf"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                now.AddDays(30),
                now.AddDays(31),
                TimeZoneId.From("UTC"));
            ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
                now.AddDays(-1),
                now.AddDays(20)));
            ticketedEvent.ClearDomainEvents();
            dbContext.TicketedEvents.Add(ticketedEvent);

            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(
                TicketTypeId, TicketTypeName.From("Conference Pass"), [], PublicCapacity, waitlistEnabled: true);
            for (var i = 1; i <= PublicCapacity; i++)
            {
                var registration = Registration.Create(
                    TeamId,
                    EventId,
                    RegisteredEmail(i),
                    FirstName.From("Registered"),
                    LastName.From($"Attendee {i}"),
                    catalog.Claim([TicketTypeId], ClaimMode.Public)); // the last claim sells out → WaitlistMode
                registration.ClearDomainEvents();
                _registeredIds.Add(registration.Id);
                dbContext.Registrations.Add(registration);
            }

            var waitlist = Waitlist.Create(EventId, TicketTypeId, TeamId);
            for (var i = 1; i <= _waitingCount; i++)
            {
                waitlist.AddEntry(WaitingEmail(i), now.AddMinutes(i), catalog, RegistrationId.New());

                var registration = Registration.Create(
                    TeamId,
                    EventId,
                    WaitingEmail(i),
                    FirstName.From("Waiting"),
                    LastName.From($"Attendee {i}"),
                    [],
                    waitlistedTickets: catalog.DescribeTicketTypes([TicketTypeId]));
                registration.ClearDomainEvents();
                _waitingIds.Add(registration.Id);
                dbContext.Registrations.Add(registration);
            }

            waitlist.ClearDomainEvents();
            dbContext.Waitlists.Add(waitlist);

            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);

            if (_withOrganiserCouponForFirstWaiting)
            {
                var coupon = new CouponBuilder()
                    .WithEventId(EventId)
                    .WithTeamId(TeamId)
                    .WithEmail(WaitingEmail(1))
                    .WithRequestedTicketTypeIds(TicketTypeId)
                    .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId))
                    .WithNow(now)
                    .WithExpiresAt(now.AddDays(7))
                    .Build();
                coupon.ClearDomainEvents();
                OrganiserCouponCode = coupon.Code.Value;
                dbContext.Coupons.Add(coupon);
            }
        });
    }
}
