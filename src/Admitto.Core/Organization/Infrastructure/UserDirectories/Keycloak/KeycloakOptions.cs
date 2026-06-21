using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Keycloak;

public class KeycloakOptions
{
    public const string SectionName = "Organization:UserDirectories:Keycloak";
    
    [Required]
    public string Authority { get; init; } = null!;
    
    [Required] 
    public string TokenPath { get; init; } = null!;

    [Required] 
    public string ClientId { get; init; } = null!;

    [Required] 
    public string Username { get; init; } = null!;

    [Required] 
    public string Password { get; init; } = null!;

    public string? ExecuteActionsClientId { get; init; }

    public string? ExecuteActionsRedirectUri { get; init; }
}
