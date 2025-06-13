using Amolenk.Admitto.Application.Common.Abstractions;

namespace Amolenk.Admitto.Application.Tests.TestFixtures;

public class IdentityFixture(IIdentityService identityService)
{
    private const string TestUserEmail = "alice@example.com";
    
    // TODO Remove if not needed
    public static readonly Guid TestUserId = new ("236d597b-a4df-4e08-b90c-b4cb1808ec2d");
    
    public IIdentityService IdentityService { get; } = identityService;
    
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
                if (user.Email != TestUserEmail)
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