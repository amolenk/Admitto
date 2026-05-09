using Amolenk.Admitto.Core.Organization.Application.Services;
using Amolenk.Admitto.Core.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.Organization.Tests.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;

internal sealed class RegisterExternalUserFixture
{
    private bool _ensureUserExistsInDatabase;
    private bool _ensureUserExistsInExternalDirectory;

    public string EmailAddress { get; } = "test@example.com";
    public Guid UserId { get; private set; }
    public Guid ExternalUserId { get; } = Guid.NewGuid();
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
                .UpsertUserAsync(EmailAddress, Arg.Any<CancellationToken>())
                .Returns(ExternalUserId);
        }

        var user = new UserBuilder()
            .WithEmailAddress(Core.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .Build();

        if (_ensureUserExistsInDatabase)
        {
            await environment.Database.SeedAsync(dbContext => { dbContext.Users.Add(user); });
        }
        
        UserId = user.Id.Value;
    }
}