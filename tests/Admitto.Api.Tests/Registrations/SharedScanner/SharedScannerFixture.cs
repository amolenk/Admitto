using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.SharedScanner;

internal sealed class SharedScannerFixture
{
    private const string DefaultEventName = "Shared Scanner Conference";
    private readonly bool _archived;
    private readonly bool _expired;
    private readonly bool _revoked;
    private readonly bool _noLink;

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid OtherEventId { get; private set; }
    public string Secret { get; private set; } = string.Empty;
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public RegistrationId CancelledRegistrationId { get; private set; } = RegistrationId.New();
    public RegistrationId OtherEventRegistrationId { get; private set; } = RegistrationId.New();

    private SharedScannerFixture(
        bool archived = false,
        bool expired = false,
        bool revoked = false,
        bool noLink = false)
    {
        _archived = archived;
        _expired = expired;
        _revoked = revoked;
        _noLink = noLink;
    }

    public static SharedScannerFixture Active() => new();
    public static SharedScannerFixture ArchivedEvent() => new(archived: true);
    public static SharedScannerFixture ExpiredLink() => new(expired: true);
    public static SharedScannerFixture RevokedLink() => new(revoked: true);
    public static SharedScannerFixture NoLink() => new(noLink: true);

    public string SessionRoute => $"/scan/{Secret}";
    public string SessionRouteFor(string secret) => $"/scan/{secret}";
    public string CheckInRoute => $"/scan/{Secret}/check-in";
    public string CheckInRouteFor(string secret) => $"/scan/{secret}/check-in";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;
        var eventId = TicketedEventId.New();
        var otherEventId = TicketedEventId.New();
        EventId = eventId.Value;
        OtherEventId = otherEventId.Value;
        var now = DateTimeOffset.UtcNow;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()), eventId, team.Id,
            EventName.From(DefaultEventName),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            _expired ? now.AddDays(-2) : now.AddDays(1),
            _expired ? now.AddMinutes(-1) : now.AddDays(2),
            TimeZoneId.From("UTC"));

        var otherEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()), otherEventId, team.Id,
            EventName.From("Other Conference"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            now.AddDays(1), now.AddDays(2), TimeZoneId.From("UTC"));

        if (!_noLink)
        {
            var secret = ScannerLinkSecret.New();
            Secret = secret.Value;
            ticketedEvent.CreateScannerLink(secret, now);

            if (_revoked)
                ticketedEvent.RevokeScannerLink(now);
        }

        if (_archived)
            ticketedEvent.Archive();

        var ticketTypeId = TicketTypeId.New();
        var registration = CreateRegistration(team.Id, eventId, "alice@example.com", "Alice", "Attendee", ticketTypeId);
        var cancelled = CreateRegistration(team.Id, eventId, "cancelled@example.com", "Cancelled", "Attendee", ticketTypeId);
        cancelled.Cancel(CancellationReason.AttendeeRequest);
        var otherRegistration = CreateRegistration(team.Id, otherEventId, "other@example.com", "Other", "Event", ticketTypeId);

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));

        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.AddRange(ticketedEvent, otherEvent);
            db.Registrations.AddRange(registration, cancelled, otherRegistration);
        });

        RegistrationId = registration.Id;
        CancelledRegistrationId = cancelled.Id;
        OtherEventRegistrationId = otherRegistration.Id;
    }

    private static Registration CreateRegistration(
        TeamId teamId,
        TicketedEventId eventId,
        string email,
        string firstName,
        string lastName,
        TicketTypeId ticketTypeId) =>
        Registration.Create(
            teamId,
            eventId,
            EmailAddress.From(email),
            FirstName.From(firstName),
            LastName.From(lastName),
            [new TicketTypeSnapshot(ticketTypeId, TicketTypeName.From("General Admission"), [])]);
}
