using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Tests;

[DoNotParallelize]
public class FullStackApiTestsBase : BasicApiTestsBase
{
    protected AuthorizationFixture AuthorizationFixture { get; private set; } = null!;
    protected DatabaseFixture DatabaseFixture { get; private set; } = null!;
    protected IdentityFixture IdentityFixture { get; private set; } = null!;
    protected QueueStorageFixture QueueStorageFixture { get; private set; } = null!;
     
    protected Team DefaultTeam = null!;

    [TestInitialize]
    public override async Task TestInitialize()
    {
        AuthorizationFixture = GlobalAppHostFixture.GetAuthorizationFixture();
        DatabaseFixture = await GlobalAppHostFixture.GetDatabaseFixtureAsync();
        IdentityFixture = GlobalAppHostFixture.GetIdentityFixture();
        QueueStorageFixture = await GlobalAppHostFixture.GetQueueStorageFixtureAsync();

        await Task.WhenAll(
            AuthorizationFixture.ResetAsync(),
            DatabaseFixture.ResetAsync(context =>
            {
                DefaultTeam = TeamDataFactory.CreateTeam(name: "Default Team");
                context.Teams.Add(DefaultTeam);
            }),
            IdentityFixture.ResetAsync(),
            QueueStorageFixture.ResetAsync());

        await base.TestInitialize();
    }
}