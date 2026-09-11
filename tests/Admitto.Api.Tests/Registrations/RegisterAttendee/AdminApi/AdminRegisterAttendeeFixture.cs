using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.RegisterAttendee.AdminApi;

internal sealed class AdminRegisterAttendeeFixture
{
    private readonly bool _bobIsCrew;

    public static readonly TicketTypeId TicketTypeId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public string Route => $"/admin/teams/{TeamId}/events/{EventId}/registrations";

    private AdminRegisterAttendeeFixture(bool bobIsCrew = false)
    {
        _bobIsCrew = bobIsCrew;
    }

    public static AdminRegisterAttendeeFixture HappyFlow() => new();
    public static AdminRegisterAttendeeFixture CrewMember() => new(bobIsCrew: true);

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
        ticketedEvent.ConfigureRegistrationPolicy(
            TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30)));

        var catalog = TicketCatalog.Create(eventId, team.Id);
        catalog.AddTicketType(TicketTypeId, TicketTypeName.From("General Admission"), [], 100);

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
        });
    }
}
