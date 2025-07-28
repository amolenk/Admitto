using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Auth;
using Amolenk.Admitto.TestHelpers.TestData;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.TestHelpers.TestFixtures;

public class IdentityTestFixture
{
    private IdentityTestFixture(IIdentityService identityService)
    {
        IdentityService = identityService;
    }
    
    public IIdentityService IdentityService { get; }
    
    public static IdentityTestFixture Create(TestingAspireAppHost appHost)
    {
        var httpClient = appHost.Application.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient("KeycloakApi");
        
        httpClient.BaseAddress = appHost.Application.GetEndpoint("keycloak");

        var identityService = new KeycloakIdentityService(httpClient);
        
        return new IdentityTestFixture(identityService);
    }
    
    public async Task ResetAsync(Func<IIdentityService, ValueTask>? seed = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all users
            var users = await IdentityService.GetUsersAsync(cancellationToken);

            // Delete all users except for the 'alice' test user we want to keep
            foreach (var user in users)
            {
                if (user.Email != UserDataFactory.TestUserEmail)
                {
                    await IdentityService.DeleteUserAsync(user.Id, cancellationToken);
                }
            }
            
            // Seed the identity service with test data
            if (seed is not null)
            {
                await seed(IdentityService);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to reset Keycloak test data", ex);
        }
    }
}