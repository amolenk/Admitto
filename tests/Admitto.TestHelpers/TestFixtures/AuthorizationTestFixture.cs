using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Auth;

namespace Amolenk.Admitto.TestHelpers.TestFixtures;

public class AuthorizationTestFixture
{
    private AuthorizationTestFixture(IRebacAuthorizationService authorizationService)
    {
        AuthorizationService = authorizationService;
    }
 
    public IRebacAuthorizationService AuthorizationService { get; }

    public static AuthorizationTestFixture Create(TestingAspireAppHost appHost)
    {
        var httpClient = appHost.CreateHttpClient("openfga", "http");

        var clientFactory = new OpenFgaClientFactory(httpClient);
        var authorizationService = new OpenFgaAuthorizationService(clientFactory);
        
        return new AuthorizationTestFixture(authorizationService);
    }
    
    public async Task ResetAsync(Func<IRebacAuthorizationService, ValueTask>? seed = null)
    {
        try
        {
            // TODO Delete all tuples
            
            // Seed the authorization service with test data
            if (seed is not null)
            {
                await seed(AuthorizationService);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to reset authorization test data", ex);
        }
    }
}