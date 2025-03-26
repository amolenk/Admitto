namespace Amolenk.Admitto.Application.UseCases.Auth;

/// <summary>
/// Represents an authorization code in an OAuth2 PKCE flow.
/// </summary>
public class AuthorizationCode
{
    public required Guid Code { get; set; }
    public required Guid UserId { get; set; }
    public required string CodeChallenge { get; set; } // PKCE challenge
    public required DateTime Expires { get; set; }
}