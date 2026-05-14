using Amolenk.Admitto.Core.Organization.Application.Services;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;

internal sealed class RegisterExternalUserFixture
{
    private bool _ensureUserExistsInDatabase;
    private bool _ensureUserExistsInExternalDirectory;

    public string EmailAddress { get; } = "test@example.com";
    public Guid UserId { get; private set; }
    public string ExternalUserId { get; } = Guid.NewGuid().ToString();
    public IExternalUserDirectory ExternalUserDirectory { get; } = Substitute.For<IExternalUserDirectory>();

    private RegisterExternalUserFixture()
    {
    }

    public static RegisterExternalUserFixture HappyFlow() => new()
    {
        _ensureUserExistsInDatabase =  true,
        _ensureUserExistsInExternalDirectory = true // WEIRD?
    };

    public static RegisterExternalUserFixture UserDoesNotExist() => new()
    {
        _ensureUserExistsInExternalDirectory = false
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_ensureUserExistsInExternalDirectory)
        {
            ExternalUserDirectory
                .InviteUserAsync(EmailAddress, Arg.Any<CancellationToken>())
                .Returns(ExternalUserId);
        }

        var user = new UserBuilder()
            .WithEmailAddress(Core.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .Build();

        if (_ensureUserExistsInDatabase)
        {
            await environment.OrganizationDatabase.SeedAsync(dbContext => { dbContext.Users.Add(user); });
        }

        UserId = user.Id.Value;
    }
}
