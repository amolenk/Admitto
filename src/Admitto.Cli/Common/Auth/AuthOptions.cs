namespace Amolenk.Admitto.Cli.Common.Auth;

public class AuthOptions
{
    public required string Authority { get; init; }
    public required string ClientId { get; init; }
    public required string Scope { get; init; }
    public required bool RequireHttps { get; init; } = true;
}
