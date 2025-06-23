using AuthorizationTestFixture = Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures.AuthorizationTestFixture;
using DatabaseTestFixture = Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures.DatabaseTestFixture;
using EmailTestFixture = Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures.EmailTestFixture;
using IdentityTestFixture = Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures.IdentityTestFixture;
using QueueStorageTestFixture = Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures.QueueStorageTestFixture;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

[TestClass]
public abstract class ApiTestsBase
{
    // Convenience properties for accessing test fixtures
    protected readonly AuthorizationTestFixture Authorization = AssemblyTestFixture.AuthorizationTestFixture;
    protected readonly DatabaseTestFixture Database = AssemblyTestFixture.DatabaseTestFixture;
    protected readonly EmailTestFixture Email = AssemblyTestFixture.EmailTestFixture;
    protected readonly IdentityTestFixture Identity = AssemblyTestFixture.IdentityTestFixture;
    protected readonly QueueStorageTestFixture QueueStorage = AssemblyTestFixture.QueueStorageTestFixture;

    protected HttpClient ApiClient { get; private set; } = null!;
        
    [TestInitialize]
    public virtual Task TestInitialize()
    {
        ApiClient = GetApiClient();

        return Task.CompletedTask;
    }
    
    private static HttpClient GetApiClient()
    {
        var application = AssemblyTestFixture.Application;
        var httpClient = application.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AdmittoApi");
        
        httpClient.BaseAddress = application.GetEndpoint("api");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        return httpClient;
    }
}
