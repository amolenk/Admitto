using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.UserDirectories.MicrosoftGraph;

public class MicrosoftGraphOptions
{
    public const string SectionName = "Organization:UserDirectories:MicrosoftGraph";
    
    [Required] 
    public required string TenantId { get; init; }

    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string ClientSecret { get; init; }
}
