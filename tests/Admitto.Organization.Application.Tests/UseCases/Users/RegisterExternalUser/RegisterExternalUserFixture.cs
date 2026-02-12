using Amolenk.Admitto.Organization.Application.Services;
using Amolenk.Admitto.Organization.Application.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.Users.RegisterExternalUser;

internal sealed class RegisterExternalUserFixture
{
    private bool _ensureUserExistsInDatabase;
    private bool _ensureUserExistsInExternalDirectory;

    public EmailAddress EmailAddress { get; } = EmailAddress.From("test@example.com");
    public UserId UserId { get; private set; }
    public ExternalUserId ExternalUserId { get; } = ExternalUserId.New();
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
                .UpsertUserAsync(EmailAddress.Value, Arg.Any<CancellationToken>())
                .Returns(ExternalUserId.Value);
        }

        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress)
            .Build();

        if (_ensureUserExistsInDatabase)
        {
            await environment.Database.SeedAsync(dbContext => { dbContext.Users.Add(user); });
        }
        
        UserId = user.Id;
    }
}