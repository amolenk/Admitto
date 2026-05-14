using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;
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

    public static ManageMembersAuthorizationFixture NoTeamMembers() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id.Value;

        var bob = new UserBuilder()
            .WithEmailAddress(EmailAddress.From("bob@example.com"))
            .WithMembership(team.Id, TeamMembershipRole.Crew)
            .Build();

        bob.AssignExternalUserId(ExternalUserId.From(BobKeycloakSub));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
            dbContext.Users.Add(bob);
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
}
