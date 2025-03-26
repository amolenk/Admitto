namespace Amolenk.Admitto.Application.UseCases.Auth;

/// <summary>
/// Represents a refresh token in an OAuth2 flow.
/// </summary>
public class RefreshToken
{
    public required Guid Token { get; set; } = Guid.NewGuid();
}