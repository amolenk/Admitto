using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistedRegistrations;

/// <summary>
/// Registration closes one hour after <see cref="Start"/>. The Conference Pass (<see cref="PublicCapacity"/> public
/// seats) is sold out to <see cref="RegisteredEmail"/> attendees and in WaitlistMode, with <see cref="WaitingCount"/>
/// attendees queued in order <see cref="WaitingEmail"/>. Each waiting attendee has a real <c>Waitlisted</c>
/// registration, and the first is also queued for the sold-out Workshop.
/// </summary>
internal sealed class WaitlistedRegistrationsFixture
{
    private readonly List<RegistrationId> _registeredIds = [];
    private readonly Dictionary<EmailAddress, RegistrationId> _waitingIds = [];

    public const int PublicCapacity = 2;
    public const int WaitingCount = 3;

    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId ConferencePassId { get; } = TicketTypeId.New();
    public TicketTypeId WorkshopId { get; } = TicketTypeId.New();

    /// <summary>The fixture's "now", while registration is open.</summary>
    public DateTimeOffset Start { get; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ClosesAt => Start.AddHours(1);

    /// <summary>After registration has closed, but before an offer issued at <see cref="Start"/> expires.</summary>
    public DateTimeOffset AfterClose => Start.AddHours(2);

    /// <summary>After registration has closed and an offer issued at <see cref="Start"/> has lapsed.</summary>
    public DateTimeOffset AfterOfferLapsed => Start.AddDays(3);

    public IReadOnlyList<RegistrationId> RegisteredIds => _registeredIds;

    public static EmailAddress RegisteredEmail(int index) => EmailAddress.From($"registered{index}@example.com");

    public static EmailAddress WaitingEmail(int position) => EmailAddress.From($"waiting{position}@example.com");

    public RegistrationId WaitingRegistrationId(int position) => _waitingIds[WaitingEmail(position)];

    private WaitlistedRegistrationsFixture()
    {
    }

    public static WaitlistedRegistrationsFixture SoldOutWithWaitlistedRegistrations() => new();

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
                Start.AddDays(30),
                Start.AddDays(31),
                TimeZoneId.From("UTC"));
            ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(Start.AddDays(-1), ClosesAt));
            ticketedEvent.ClearDomainEvents();
            dbContext.TicketedEvents.Add(ticketedEvent);

            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(
                ConferencePassId, TicketTypeName.From("Conference Pass"), [], PublicCapacity, waitlistEnabled: true);
            catalog.AddTicketType(WorkshopId, TicketTypeName.From("Workshop"), [], 1, waitlistEnabled: true);

            for (var i = 1; i <= PublicCapacity; i++)
            {
                // The last Conference Pass claim sells out and activates WaitlistMode; the first one also takes
                // the only Workshop seat.
                var ticketTypeIds = i == 1 ? new[] { ConferencePassId, WorkshopId } : [ConferencePassId];
                var registration = Registration.Create(
                    TeamId,
                    EventId,
                    RegisteredEmail(i),
                    FirstName.From("Registered"),
                    LastName.From($"Attendee {i}"),
                    catalog.Claim(ticketTypeIds, ClaimMode.Public));
                registration.ClearDomainEvents();
                _registeredIds.Add(registration.Id);
                dbContext.Registrations.Add(registration);
            }

            var conferencePassWaitlist = Waitlist.Create(EventId, ConferencePassId, TeamId);
            var workshopWaitlist = Waitlist.Create(EventId, WorkshopId, TeamId);
            for (var position = 1; position <= WaitingCount; position++)
            {
                var email = WaitingEmail(position);
                conferencePassWaitlist.AddEntry(email, Start.AddMinutes(position - 60), catalog, RegistrationId.New());
                IReadOnlyList<TicketTypeId> waitlistedIds = position == 1 ? [ConferencePassId, WorkshopId] : [ConferencePassId];
                if (position == 1)
                    workshopWaitlist.AddEntry(email, Start.AddMinutes(-60), catalog, RegistrationId.New());

                var registration = Registration.Create(
                    TeamId,
                    EventId,
                    email,
                    FirstName.From("Waiting"),
                    LastName.From($"Attendee {position}"),
                    [],
                    waitlistedTickets: catalog.DescribeTicketTypes(waitlistedIds));
                registration.ClearDomainEvents();
                _waitingIds[email] = registration.Id;
                dbContext.Registrations.Add(registration);
            }

            conferencePassWaitlist.ClearDomainEvents();
            workshopWaitlist.ClearDomainEvents();
            dbContext.Waitlists.AddRange(conferencePassWaitlist, workshopWaitlist);

            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
