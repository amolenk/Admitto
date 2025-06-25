# Entra ID Identity Provider

This document describes how to configure and use the Entra ID identity provider as an alternative to Keycloak.

## Configuration

To use Entra ID as the identity provider, update your application configuration:

### 1. Set the Identity Provider

```json
{
  "IdentityProvider": {
    "Provider": "EntraId"
  }
}
```

### 2. Configure Entra ID Settings

```json
{
  "EntraId": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id", 
    "ClientSecret": "your-client-secret"
  }
}
```

## Required Permissions

The Entra ID application registration must have the following Microsoft Graph API permissions:

- **User.ReadWrite.All** (Application permission)
- **Directory.ReadWrite.All** (Application permission)

## Implementation Details

The `EntraIdIdentityService` implements the same `IIdentityService` interface as the Keycloak provider:

- **GetUserByEmailAsync**: Searches for users by email using Microsoft Graph
- **GetUsersAsync**: Retrieves all enabled users from the tenant
- **AddUserAsync**: Creates a new user with a temporary password
- **DeleteUserAsync**: Removes a user from the tenant

### Key Differences from Keycloak

1. **Authentication**: Uses client credentials flow with Azure.Identity
2. **User Creation**: Creates users with temporary passwords that must be changed on first login
3. **User IDs**: Microsoft Graph returns string IDs that are converted to GUIDs
4. **Error Handling**: Uses Microsoft Graph's ServiceException for API errors

## Environment Variables

For production deployments, you can use environment variables:

```bash
IdentityProvider__Provider=EntraId
EntraId__TenantId=your-tenant-id
EntraId__ClientId=your-client-id
EntraId__ClientSecret=your-client-secret
```

## Migration from Keycloak

To migrate from Keycloak to Entra ID:

1. Update the configuration as shown above
2. Ensure your Entra ID tenant has the required permissions
3. Restart the application - the dependency injection will automatically use the Entra ID provider
4. Existing user data in your application database remains unchanged

## Testing

Unit tests for the Entra ID service are located in `tests/Admitto.Infrastructure.Tests/Auth/EntraIdIdentityServiceTests.cs`.

To test the configuration switching, you can run integration tests with different configuration values.