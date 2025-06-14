using Amolenk.Admitto.TestHelpers.TestFixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Api.Tests;

public class ApiTestsBase
{
    protected AuthorizationTestFixture Authorization = AssemblyTestFixture.Authorization;
    protected DatabaseTestFixture Database = AssemblyTestFixture.Database;
    protected EmailTestFixture Email = AssemblyTestFixture.Email;
    protected IdentityTestFixture Identity = AssemblyTestFixture.Identity;
    protected QueueStorageTestFixture QueueStorage = AssemblyTestFixture.QueueStorage;

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
