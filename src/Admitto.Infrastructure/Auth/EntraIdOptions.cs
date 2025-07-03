using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Infrastructure.Auth;

public class EntraIdOptions
{
    [Required] public string TenantId { get; set; } = string.Empty;

    [Required] public string ClientId { get; set; } = string.Empty;

    [Required] public string ClientSecret { get; set; } = string.Empty;

    public string Scope { get; set; } = "https://graph.microsoft.com/.default";

    public string Authority => $"https://login.microsoftonline.com/{TenantId}";
}