namespace Amolenk.Admitto.Core.IntegrationTests;

public abstract class AspireIntegrationTestBase
{
    public static IntegrationTestEnvironment Environment { get; set; } = null!;

    [TestInitialize]
    public virtual async ValueTask TestInitialize()
    {
        await Environment.BadgesDatabase.ResetAsync();
        await Environment.EmailDatabase.ResetAsync();
        await Environment.OrganizationDatabase.ResetAsync();
        await Environment.RegistrationsDatabase.ResetAsync();
    }
}
