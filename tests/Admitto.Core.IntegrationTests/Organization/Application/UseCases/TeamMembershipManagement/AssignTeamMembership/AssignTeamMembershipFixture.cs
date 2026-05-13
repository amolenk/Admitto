using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership;

internal sealed class AssignTeamMembershipFixture
{
    public Guid TeamId { get; private set; }
    public string EmailAddress { get; } = "test@example.com";
    public Guid UserId { get; private set; }

    private bool _seedUser;

    private AssignTeamMembershipFixture()
    {
    }

    public static AssignTeamMembershipFixture TeamOnly() => new();

    public static AssignTeamMembershipFixture UserExists() => new() { _seedUser = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);

            if (_seedUser)
            {
                var user = new UserBuilder()
                    .WithEmailAddress(Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
                    .Build();

                dbContext.Users.Add(user);
                UserId = user.Id.Value;
            }
        });
    }
}
