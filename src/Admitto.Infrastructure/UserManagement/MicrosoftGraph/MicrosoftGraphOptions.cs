using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Infrastructure.UserManagement.MicrosoftGraph;

public class MicrosoftGraphOptions
{
    public const string SectionName = "UserManagement:MicrosoftGraph";
    
    [Required] 
    public required string TenantId { get; init; }

    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string ClientSecret { get; init; }
}
