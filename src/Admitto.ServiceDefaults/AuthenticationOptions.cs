namespace Amolenk.Admitto.ServiceDefaults;

public class AuthenticationOptions
{
    public required string Authority { get; init; }
    public required string Audience { get; init; }
    public required bool RequireHttpsMetadata { get; init; } = true;
    public required string[] ValidIssuers { get; init; }
}