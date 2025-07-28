namespace Amolenk.Admitto.ServiceDefaults;

public class AuthenticationOptions
{
    public required string Authority { get; init; } = "https://login.microsoftonline.com/3491bee1-dc92-4c78-9193-2209b34dc958/v2.0";
    public required string Audience { get; init; } = "api://a2cdf1a1-694b-4f79-be1a-ebe5f1f04bf7";
    public required bool RequireHttpsMetadata { get; init; } = true;
    public string[] ValidIssuers { get; init; } =
    [
        // Personal Microsoft accounts use v1 tokens.
        "https://sts.windows.net/3491bee1-dc92-4c78-9193-2209b34dc958/",
        // Work or school Microsoft accounts use v2 tokens.
        "https://login.microsoftonline.com/3491bee1-dc92-4c78-9193-2209b34dc958/v2.0"
    ];
}