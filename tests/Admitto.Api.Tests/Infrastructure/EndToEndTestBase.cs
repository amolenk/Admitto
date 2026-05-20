using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;

namespace Amolenk.Admitto.Api.Tests.Infrastructure;

public abstract class EndToEndTestBase
{
    private const string AliceKeycloakSub = "236d597b-a4df-4e08-b90c-b4cb1808ec2d";
    private const string BobKeycloakSub = "6189cd5b-6b08-4ff1-a87d-4e434e8d1c79";

    internal static EndToEndTestEnvironment Environment { get; set; } = null!;

    [TestInitialize]
    public virtual async ValueTask TestInitialize()
    {
        await Environment.Messaging.ResetAsync();
        await Environment.OrganizationDatabase.ResetAsync();
        await Environment.RegistrationsDatabase.ResetAsync();
        await Environment.EmailDatabase.ResetAsync();
        await Environment.BadgesDatabase.ResetAsync();
        await Environment.Email.ResetAsync();

        await SeedUserAsync(AliceKeycloakSub, configure => configure
            .WithEmailAddress(EmailAddress.From("alice@example.com"))
            .WithIsAdmin());

        await SeedUserAsync(BobKeycloakSub, configure => configure
            .WithEmailAddress(EmailAddress.From("bob@example.com")));
    }

    private static async ValueTask SeedUserAsync(string externalUserId, Action<UserBuilder> configure)
    {
        var userBuilder = new UserBuilder();
        configure(userBuilder);

        var user = userBuilder.Build();
        user.AssignExternalUserId(ExternalUserId.From(externalUserId));

        await Environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });
    }
}
