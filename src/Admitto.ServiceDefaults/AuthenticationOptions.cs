namespace Amolenk.Admitto.ServiceDefaults;

public class AuthenticationOptions
{
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; }
}