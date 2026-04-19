using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure.Hosting;

namespace Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;

public abstract class AspireIntegrationTestBase
{
    public static IntegrationTestEnvironment Environment { get; set; } = null!;

    [TestInitialize]
    public virtual async ValueTask TestInitialize()
    {
        await Environment.Database.ResetAsync();
    }
}
