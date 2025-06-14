using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.TestHelpers.TestData;

namespace Amolenk.Admitto.Api.Tests;

[DoNotParallelize]
public class FullStackApiTestsBase : ApiTestsBase
{
    protected Team DefaultTeam = null!;
    
    [TestInitialize]
    public override async Task TestInitialize()
    {
        await Task.WhenAll(
            AssemblyTestFixture.Authorization.ResetAsync(),
            AssemblyTestFixture.Database.ResetAsync(context =>
            {
                DefaultTeam = TeamDataFactory.CreateTeam(name: "Default Team",
                    emailSettings: AssemblyTestFixture.Email.DefaultEmailSettings);

                context.Teams.Add(DefaultTeam);
            }),
            AssemblyTestFixture.Identity.ResetAsync(),
            AssemblyTestFixture.QueueStorage.ResetAsync());

        await base.TestInitialize();
    }
}