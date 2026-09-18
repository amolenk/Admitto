using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.CheckIn;

internal sealed class CheckInFixture
{
    private const string CheckInEventName = "Check-in Conference";
    private readonly TeamMembershipRole? _bobRole;
    private readonly bool _archived;
    private readonly bool _summary;
    private readonly bool _manyCandidates;

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid OtherEventId { get; private set; }
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public RegistrationId CancelledRegistrationId { get; private set; } = RegistrationId.New();
    public RegistrationId OtherEventRegistrationId { get; private set; } = RegistrationId.New();

    private CheckInFixture(
        TeamMembershipRole? bobRole = null,
        bool archived = false,
        bool summary = false,
        bool manyCandidates = false)
    {
        _bobRole = bobRole;
        _archived = archived;
        _summary = summary;
        _manyCandidates = manyCandidates;
    }

    public static CheckInFixture Active() => new();
    public static CheckInFixture Archived() => new(archived: true);
    public static CheckInFixture Summary() => new(summary: true);
    public static CheckInFixture LookupCandidates() => new(manyCandidates: true);
    public static CheckInFixture BobIsCrew() => new(TeamMembershipRole.Crew);
    public static CheckInFixture BobIsOrganizer() => new(TeamMembershipRole.Organizer);
    public static CheckInFixture BobIsOwner() => new(TeamMembershipRole.Owner);

    public string CheckInRoute => $"/admin/teams/{TeamId}/events/{EventId}/registrations/check-in";
    public string LookupRoute(string query) =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations/check-in/lookup?query={Uri.EscapeDataString(query)}";
    public string SummaryRoute => $"/admin/teams/{TeamId}/events/{EventId}/registrations/check-in/summary";
    public string DetailRoute => $"/admin/teams/{TeamId}/events/{EventId}/registrations/{RegistrationId.Value}";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;
        var eventId = TicketedEventId.New();
        var otherEventId = TicketedEventId.New();
        EventId = eventId.Value;
        OtherEventId = otherEventId.Value;

        var ticketedEvent = CreateEvent(eventId, team.Id, CheckInEventName);
        var otherEvent = CreateEvent(otherEventId, team.Id, "Other Conference");
        if (_archived)
            ticketedEvent.Archive();

        var ticketTypeId = TicketTypeId.New();
        var registration = CreateRegistration(team.Id, eventId, "alice@example.com", "Alice", "Attendee", ticketTypeId);
        var cancelled = CreateRegistration(team.Id, eventId, "cancelled@example.com", "Cancelled", "Attendee", ticketTypeId);
        cancelled.Cancel(CancellationReason.AttendeeRequest);
        var otherRegistration = CreateRegistration(team.Id, otherEventId, "other@example.com", "Other", "Event", ticketTypeId);

        if (_summary)
            registration.CheckIn(DateTimeOffset.UtcNow);

        var bob = _bobRole is null
            ? null
            : await environment.OrganizationDatabase.Context.Users.GetAsync(
                u => u.EmailAddress == EmailAddress.From("bob@example.com"));

        if (bob is not null)
        {
            var creationRequest = team.RequestEventCreation(
                EventName.From(CheckInEventName),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                Slug.From("check-in-conference"),
                ticketedEvent.StartsAt,
                ticketedEvent.EndsAt,
                TimeZoneId.From("UTC"),
                bob.Id,
                DateTimeOffset.UtcNow);
            team.RegisterEventCreated(creationRequest.Id, eventId, DateTimeOffset.UtcNow);
        }

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            if (bob is not null)
                bob.AddTeamMembership(team.Id, _bobRole!.Value);
            db.Teams.Add(team);
        });

        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.AddRange(ticketedEvent, otherEvent);
            db.Registrations.AddRange(registration, cancelled, otherRegistration);

            if (_summary)
            {
                var checkedIn = CreateRegistration(team.Id, eventId, "checked@example.com", "Checked", "In", ticketTypeId);
                checkedIn.CheckIn(DateTimeOffset.UtcNow);
                db.Registrations.Add(checkedIn);
            }

            if (_manyCandidates)
            {
                for (var i = 0; i < 25; i++)
                {
                    db.Registrations.Add(CreateRegistration(
                        team.Id,
                        eventId,
                        $"candidate{i}@example.com",
                        "Candidate",
                        $"{i:00}",
                        ticketTypeId));
                }
            }
        });

        RegistrationId = registration.Id;
        CancelledRegistrationId = cancelled.Id;
        OtherEventRegistrationId = otherRegistration.Id;
    }

    private static TicketedEvent CreateEvent(TicketedEventId id, TeamId teamId, string name) =>
        TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            id,
            teamId,
            EventName.From(name),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            TimeZoneId.From("UTC"));

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
