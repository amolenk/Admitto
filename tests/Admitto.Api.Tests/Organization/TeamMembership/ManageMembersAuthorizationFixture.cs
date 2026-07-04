using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Organization.TeamMembership;

internal sealed class ManageMembersAuthorizationFixture
{
    // Bob's Keycloak sub (JWT "sub" claim) from the test realm.
    public static readonly string BobKeycloakSub = "6189cd5b-6b08-4ff1-a87d-4e434e8d1c79";

    public Guid TeamId { get; private set; }

    private ManageMembersAuthorizationFixture() { }

    public string MembersRoute => $"/admin/teams/{TeamId}/members";

    public static ManageMembersAuthorizationFixture BobIsCrewMember() => new();

    public static ManageMembersAuthorizationFixture BobIsOwnerOfDifferentTeam() => new();

    public static ManageMembersAuthorizationFixture NoTeamMembers() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id.Value;

        var bob = await environment.OrganizationDatabase.Context.Users.GetAsync(u =>
            u.EmailAddress == EmailAddress.From("bob@example.com"));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            bob.AddTeamMembership(team.Id, TeamMembershipRole.Crew);
            // bob.AssignExternalUserId(ExternalUserId.From(BobKeycloakSub));

            dbContext.Teams.Add(team);
        });
    }

    public async ValueTask SetupTeamOnlyAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id.Value;

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });
    }

    public async ValueTask SetupWithOtherTeamMembershipAsync(EndToEndTestEnvironment environment)
    {
        var requestedTeam = new TeamBuilder().Build();
        var otherTeam = new TeamBuilder().Build();
        TeamId = requestedTeam.Id.Value;

        var bob = await environment.OrganizationDatabase.Context.Users.GetAsync(u =>
            u.EmailAddress == EmailAddress.From("bob@example.com"));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            bob.AddTeamMembership(otherTeam.Id, TeamMembershipRole.Owner);

            dbContext.Teams.AddRange(requestedTeam, otherTeam);
        });
    }
}
