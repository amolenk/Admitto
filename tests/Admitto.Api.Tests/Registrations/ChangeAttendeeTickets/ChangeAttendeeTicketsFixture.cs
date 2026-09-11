using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.ChangeAttendeeTickets;

internal sealed class ChangeAttendeeTicketsFixture
{
    private readonly bool _bobIsCrew;

    public static readonly TicketTypeId GeneralAdmissionId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public static readonly TicketTypeId WorkshopId = TicketTypeId.From(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    public string Route =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations/{RegistrationId.Value}/tickets";

    private ChangeAttendeeTicketsFixture(bool bobIsCrew = false)
    {
        _bobIsCrew = bobIsCrew;
    }

    public static ChangeAttendeeTicketsFixture WithActiveRegistration() => new();
    public static ChangeAttendeeTicketsFixture WithCrewMember() => new(bobIsCrew: true);

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        var catalog = TicketCatalog.Create(eventId, team.Id);
        catalog.AddTicketType(GeneralAdmissionId, TicketTypeName.From("General Admission"), [], 100);
        catalog.AddTicketType(WorkshopId, TicketTypeName.From("Workshop"), [], 20);

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Test"),
            [new TicketTypeSnapshot(GeneralAdmissionId, TicketTypeName.From("General Admission"), [])]);
        RegistrationId = registration.Id;

        var bob = _bobIsCrew
            ? await environment.OrganizationDatabase.Context.Users.GetAsync(
                u => u.EmailAddress == EmailAddress.From("bob@example.com"))
            : null;

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            if (bob is not null)
            {
                bob.AddTeamMembership(team.Id, TeamMembershipRole.Crew);
                var creationRequest = team.RequestEventCreation(bob.Id, DateTimeOffset.UtcNow);
                team.RegisterEventCreated(creationRequest.Id, eventId, DateTimeOffset.UtcNow);
            }

            db.Teams.Add(team);
        });
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            db.Registrations.Add(registration);
        });
    }
}
