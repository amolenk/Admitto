using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;

namespace Amolenk.Admitto.Api.Tests.Organization.TicketedEventManagement;

internal sealed class CreateTicketedEventFixture
{
    public Guid TeamId { get; private set; }

    private CreateTicketedEventFixture() { }

    public string EventCreationsRoute => $"/admin/teams/{TeamId}/events";

    public string EventCreationStatusRoute(string creationRequestId) =>
        $"/admin/teams/{TeamId}/event-creations/{creationRequestId}";

    public static CreateTicketedEventFixture WithTeam() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id.Value;

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });
    }
}
