using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Amolenk.Admitto.Module.Organization.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Api.Auth;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOrganizationFacade organizationFacade)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string TeamIdClaimType = "team_id";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyValues))
        {
            return AuthenticateResult.NoResult();
        }

        var rawKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return AuthenticateResult.NoResult();
        }

        var keyHash = ComputeHash(rawKey);
        var teamId = await organizationFacade.ValidateApiKeyAsync(keyHash, Context.RequestAborted);

        if (teamId is null)
        {
            return AuthenticateResult.NoResult();
        }

        var claims = new[]
        {
            new Claim(TeamIdClaimType, teamId.Value.ToString())
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    private static string ComputeHash(string rawKey)
    {
        var bytes = Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
