namespace Amolenk.Admitto.Application.Tests;

public class BasicApiTestsBase
{
    protected HttpClient ApiClient { get; private set; } = null!;
        
    [TestInitialize]
    public virtual Task TestInitialize()
    {
        ApiClient = GlobalAppHostFixture.GetApiClient();

        return Task.CompletedTask;
    }
}