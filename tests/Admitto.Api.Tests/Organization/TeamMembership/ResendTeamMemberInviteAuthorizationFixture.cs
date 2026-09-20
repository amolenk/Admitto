using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Organization.TeamMembership;

internal sealed class ResendTeamMemberInviteAuthorizationFixture
{
    public Guid TeamId { get; private set; }
    public string MemberEmail => "alice@example.com";

    private ResendTeamMemberInviteAuthorizationFixture() { }

    public string ResendInviteRoute => $"/admin/teams/{TeamId}/members/{MemberEmail}/resend-invite";

    public static ResendTeamMemberInviteAuthorizationFixture BobIsCrewMember() => new();

    public static ResendTeamMemberInviteAuthorizationFixture BobIsOwnerOfDifferentTeam() => new();

    public static ResendTeamMemberInviteAuthorizationFixture BobIsOwner() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;

        var bob = await environment.OrganizationDatabase.Context.Users.GetAsync(u =>
            u.EmailAddress == EmailAddress.From("bob@example.com"));
        var alice = await environment.OrganizationDatabase.Context.Users.GetAsync(u =>
            u.EmailAddress == EmailAddress.From(MemberEmail));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            bob.AddTeamMembership(team.Id, TeamMembershipRole.Crew);
            alice.AddTeamMembership(team.Id, TeamMembershipRole.Crew);

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
        var alice = await environment.OrganizationDatabase.Context.Users.GetAsync(u =>
            u.EmailAddress == EmailAddress.From(MemberEmail));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            bob.AddTeamMembership(otherTeam.Id, TeamMembershipRole.Owner);
            alice.AddTeamMembership(requestedTeam.Id, TeamMembershipRole.Crew);

            dbContext.Teams.AddRange(requestedTeam, otherTeam);
        });
    }

    public async ValueTask SetupWithBobAsOwnerAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;

        var bob = await environment.OrganizationDatabase.Context.Users.GetAsync(u =>
            u.EmailAddress == EmailAddress.From("bob@example.com"));
        var alice = await environment.OrganizationDatabase.Context.Users.GetAsync(u =>
            u.EmailAddress == EmailAddress.From(MemberEmail));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            bob.AddTeamMembership(team.Id, TeamMembershipRole.Owner);
            alice.AddTeamMembership(team.Id, TeamMembershipRole.Crew);

            dbContext.Teams.Add(team);
        });
    }
}
