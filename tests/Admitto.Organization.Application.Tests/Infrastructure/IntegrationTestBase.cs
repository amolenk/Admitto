using Amolenk.Admitto.Organization.Application.Tests.Infrastructure.Hosting;

namespace Amolenk.Admitto.Organization.Application.Tests.Infrastructure;

public abstract class AspireIntegrationTestBase
{
    public static IntegrationTestEnvironment Environment { get; set; } = null!;

    [TestInitialize]
    public virtual async ValueTask TestInitialize()
    {
        await Environment.Database.ResetAsync();
    }
}