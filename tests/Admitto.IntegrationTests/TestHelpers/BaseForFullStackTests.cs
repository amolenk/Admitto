using Amolenk.Admitto.Domain.Entities;
using TeamDataFactory = Amolenk.Admitto.IntegrationTests.TestHelpers.Data.TeamDataFactory;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

[DoNotParallelize]
public abstract class BaseForFullStackTests : BaseForApiTests
{
    protected Team DefaultTeam = null!;
    
    [TestInitialize]
    public override async Task TestInitialize()
    {
        await Task.WhenAll(
            Authorization.ResetAsync(),
            Database.ResetAsync(context =>
            {
                DefaultTeam = TeamDataFactory.CreateTeam(name: "Default Team", 
                    emailSettings: Email.DefaultEmailSettings);

                context.Teams.Add(DefaultTeam);
            }),
            Identity.ResetAsync(),
            QueueStorage.ResetAsync());

        await base.TestInitialize();
    }
}