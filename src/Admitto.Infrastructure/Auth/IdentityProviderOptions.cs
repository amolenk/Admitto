namespace Amolenk.Admitto.Infrastructure.Auth;

public class IdentityProviderOptions
{
    public string Provider { get; set; } = "Keycloak"; // Default to Keycloak for backward compatibility
}

public static class IdentityProviders
{
    public const string Keycloak = "Keycloak";
    public const string EntraId = "EntraId";
}