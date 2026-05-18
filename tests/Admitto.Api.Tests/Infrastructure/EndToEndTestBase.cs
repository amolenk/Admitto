using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;

namespace Amolenk.Admitto.Api.Tests.Infrastructure;

public abstract class EndToEndTestBase
{
    // Alice's Keycloak sub (JWT "sub" claim) from the test realm — must match the test user.
    public static readonly string AliceKeycloakSub = "236d597b-a4df-4e08-b90c-b4cb1808ec2d";

    private const string ServiceBusQueueName = "queue";

    internal static EndToEndTestEnvironment Environment { get; set; } = null!;

    [TestInitialize]
    public virtual async ValueTask TestInitialize()
    {
        // Drain the queue first so any in-flight worker activity (including the
        // BootstrapAdminInitializer on a worker restart) has settled before we
        // wipe the databases. This prevents a race where the initializer inserts
        // Alice after our reset but before SeedAliceAsync.
        await PurgeServiceBusQueueAsync();

        await Environment.OrganizationDatabase.ResetAsync();
        await Environment.RegistrationsDatabase.ResetAsync();
        await Environment.EmailDatabase.ResetAsync();

        await Environment.ClearAsync(CancellationToken.None);

        await SeedAliceAsync();
    }

    private static async Task PurgeServiceBusQueueAsync()
    {
        await Environment.ServiceBusAdministrationClient.DeleteQueueAsync(ServiceBusQueueName);
        await Environment.ServiceBusAdministrationClient.CreateQueueAsync(ServiceBusQueueName);
    }

    private static async ValueTask SeedAliceAsync()
    {
        var alice = new UserBuilder()
            .WithEmailAddress(EmailAddress.From("alice@example.com"))
            .WithIsAdmin()
            .Build();

        alice.AssignExternalUserId(ExternalUserId.From(AliceKeycloakSub));

        await Environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(alice);
        });
    }
}
