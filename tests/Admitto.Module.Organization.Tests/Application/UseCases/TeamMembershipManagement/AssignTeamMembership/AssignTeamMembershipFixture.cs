using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamMembershipManagement.AssignTeamMembership;

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

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);

            if (_seedUser)
            {
                var user = new UserBuilder()
                    .WithEmailAddress(Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
                    .Build();

                dbContext.Users.Add(user);
                UserId = user.Id.Value;
            }
        });
    }
}