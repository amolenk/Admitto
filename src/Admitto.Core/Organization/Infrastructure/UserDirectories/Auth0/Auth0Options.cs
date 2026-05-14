using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Auth0;

public sealed class Auth0Options
{
    public const string SectionName = "Organization:UserDirectories:Auth0";

    [Required]
    public string Domain { get; set; } = "";

    [Required]
    public string ClientId { get; set; } = "";

    [Required]
    public string ClientSecret { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";
}
