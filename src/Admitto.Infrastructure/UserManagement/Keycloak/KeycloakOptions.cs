using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Infrastructure.UserManagement.Keycloak;

public class KeycloakOptions
{
    public const string SectionName = "UserManagement:Keycloak";
    
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
}
