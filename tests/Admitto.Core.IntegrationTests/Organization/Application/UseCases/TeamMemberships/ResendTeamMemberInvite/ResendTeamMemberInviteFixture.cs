using Amolenk.Admitto.Core.Organization.Application.ExternalUsers;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.ResendTeamMemberInvite;

internal sealed class ResendTeamMemberInviteFixture
{
    private bool _ensureUserExistsInDatabase;
    private bool _memberOfRequestedTeam;

    public Guid TeamId { get; } = Guid.NewGuid();
    public Guid OtherTeamId { get; } = Guid.NewGuid();
    public string EmailAddress { get; } = "alice@example.com";
    public Guid UserId { get; private set; }
    public string ExternalUserId { get; } = Guid.NewGuid().ToString();
    public IExternalUserDirectory ExternalUserDirectory { get; } = Substitute.For<IExternalUserDirectory>();

    private ResendTeamMemberInviteFixture()
    {
    }

    public static ResendTeamMemberInviteFixture MemberOfTheTeam() => new()
    {
        _ensureUserExistsInDatabase = true,
        _memberOfRequestedTeam = true
    };

    public static ResendTeamMemberInviteFixture MemberOfAnotherTeamOnly() => new()
    {
        _ensureUserExistsInDatabase = true,
        _memberOfRequestedTeam = false
    };

    public static ResendTeamMemberInviteFixture UserDoesNotExist() => new()
    {
        _ensureUserExistsInDatabase = false
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        ExternalUserDirectory
            .InviteUserAsync(EmailAddress, Arg.Any<CancellationToken>())
            .Returns(ExternalUserId);

        var emailAddress = global::Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress);
        var builder = new UserBuilder().WithEmailAddress(emailAddress);

        if (_memberOfRequestedTeam)
        {
            builder.WithMembership(
                global::Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId.From(TeamId),
                TeamMembershipRole.Crew);
        }
        else
        {
            builder.WithMembership(
                global::Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId.From(OtherTeamId),
                TeamMembershipRole.Crew);
        }

        var user = builder.Build();
        UserId = user.Id.Value;

        if (_ensureUserExistsInDatabase)
        {
            await environment.OrganizationDatabase.SeedAsync(dbContext => { dbContext.Users.Add(user); });
        }
    }
}
