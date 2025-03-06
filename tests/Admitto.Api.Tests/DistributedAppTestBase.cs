using Amolenk.Admitto.Infrastructure.Persistence;

namespace Amolenk.Admitto.Application.Tests;

public abstract class DistributedAppTestBase
{
    protected ApplicationContext Context { get; private set; } = null!;

    protected HttpClient Api { get; private set; } = null!;
    
    [TestInitialize]
    public async ValueTask TestInitialize()
    {
        // Create a new instance of the database context.
        Context = DistributedAppTestContext.CreateApplicationContext();
        
        // Reset the database to initial state.
        await Context.Database.EnsureDeletedAsync();
        await Context.Database.EnsureCreatedAsync();
        
        // Create an HttpClient to call the API.
        Api = DistributedAppTestContext.CreateApiClient();
    }

    [TestCleanup]
    public async ValueTask TestCleanup()
    {
        await Context.DisposeAsync();
        Api.Dispose();
    }
}
