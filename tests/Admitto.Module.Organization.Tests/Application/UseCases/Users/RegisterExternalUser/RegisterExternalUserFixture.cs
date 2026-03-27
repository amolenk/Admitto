using Amolenk.Admitto.Module.Organization.Application.Services;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.Users.RegisterExternalUser;

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
            .WithEmailAddress(Module.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .Build();

        if (_ensureUserExistsInDatabase)
        {
            await environment.Database.SeedAsync(dbContext => { dbContext.Users.Add(user); });
        }
        
        UserId = user.Id.Value;
    }
}