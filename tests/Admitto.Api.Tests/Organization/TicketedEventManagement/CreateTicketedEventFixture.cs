using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;

namespace Amolenk.Admitto.Api.Tests.Organization.TicketedEventManagement;

internal sealed class CreateTicketedEventFixture
{
    private const string TeamSlug = "test-team";

    private CreateTicketedEventFixture() { }

    public static string EventCreationsRoute => $"/admin/teams/{TeamSlug}/events";

    public static string EventCreationStatusRoute(string creationRequestId) =>
        $"/admin/teams/{TeamSlug}/event-creations/{creationRequestId}";

    public static CreateTicketedEventFixture WithTeam() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithSlug(TeamSlug)
            .Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });
    }
}
