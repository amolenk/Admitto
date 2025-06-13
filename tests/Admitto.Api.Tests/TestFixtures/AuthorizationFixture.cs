using Amolenk.Admitto.Application.Common.Abstractions;

namespace Amolenk.Admitto.Application.Tests.TestFixtures;

public class AuthorizationFixture(IRebacAuthorizationService authorizationService)
{
    public IRebacAuthorizationService AuthorizationService => authorizationService;
    
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