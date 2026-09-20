using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Api.Tests.Registrations.TicketedEvents.ScannerLinks;

internal sealed class ScannerLinksFixture
{
    private readonly bool _bobIsCrew;

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public string Route => $"/admin/teams/{TeamId}/events/{EventId}/scanner-link";
    public string RegenerateRoute => $"{Route}/regenerate";
    public string RevokeRoute => $"{Route}/revoke";

    private ScannerLinksFixture(bool bobIsCrew = false) => _bobIsCrew = bobIsCrew;

    public static ScannerLinksFixture Organizer() => new();
    public static ScannerLinksFixture CrewMember() => new(bobIsCrew: true);

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;
        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()), eventId, team.Id,
            EventName.From("Scanner Event"), AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"), DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(2), TimeZoneId.From("UTC"));

        var bob = _bobIsCrew
            ? await environment.OrganizationDatabase.Context.Users.GetAsync(
                u => u.EmailAddress == EmailAddress.From("bob@example.com"))
            : null;

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            if (bob is not null)
            {
                bob.AddTeamMembership(team.Id, TeamMembershipRole.Crew);
                var request = team.RequestEventCreation(bob.Id, DateTimeOffset.UtcNow);
                team.RegisterEventCreated(request.Id, eventId, DateTimeOffset.UtcNow);
            }

            db.Teams.Add(team);
        });

        await environment.RegistrationsDatabase.SeedAsync(db => db.TicketedEvents.Add(ticketedEvent));
    }
}
