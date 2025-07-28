namespace Amolenk.Admitto.Cli.Services;

public class CliAuthOptions
{
    public required string Authority { get; init; } = "https://login.microsoftonline.com/3491bee1-dc92-4c78-9193-2209b34dc958/v2.0";
    public required string ClientId { get; init; } = "ffeca9b6-fa75-4f7d-bb30-feecb1ef0842";
    public required string Scope { get; init; } = "api://a2cdf1a1-694b-4f79-be1a-ebe5f1f04bf7/Admin offline_access";
    public required bool RequireHttps { get; init; } = true;
}
