namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

[TestClass]
public abstract class ApiTestsBase
{
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
        
        // Use the configured HttpClient because it has an access token handler
        var httpClient = application.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AdmittoApi");
        
        httpClient.BaseAddress = application.GetEndpoint("api");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        return httpClient;
    }
}
