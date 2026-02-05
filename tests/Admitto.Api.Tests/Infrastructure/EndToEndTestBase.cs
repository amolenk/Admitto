using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;

namespace Amolenk.Admitto.Api.Tests.Infrastructure;

public abstract class EndToEndTestBase
{
    internal static EndToEndTestEnvironment Environment { get; set; } = null!;

    [TestInitialize]
    public virtual async ValueTask TestInitialize()
    {
        await Environment.OrganizationDatabase.ResetAsync();
        await Environment.RegistrationsDatabase.ResetAsync();
    }
}