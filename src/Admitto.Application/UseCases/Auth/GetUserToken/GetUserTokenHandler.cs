// using System.Security.Claims;
// using System.Security.Cryptography;
// using System.Text;
// using Amolenk.Admitto.Application.UseCases.Accounts.GetUserToken;
// using Amolenk.Admitto.Application.UseCases.Auth;
//
// namespace Amolenk.Admitto.Application.UseCases.Identity.GetUserToken;
//
// /// <summary>
// /// Gets JWT access and refresh tokens for the OAuth 2.0 PKCE flow.
// /// </summary>
// public class GetUserTokenHandler(IAuthContext context, TokenService tokenService)
//     : ICommandHandler<GetUserTokenCommand, GetUserTokenResult>
// {
//     public async ValueTask<GetUserTokenResult> HandleAsync(GetUserTokenCommand command, CancellationToken cancellationToken)
//     {
//         // Check for a valid authorization code.
//         var authorizationCode = await context.AuthorizationCodes.FindAsync([command.Code], cancellationToken);
//         if (authorizationCode is null || authorizationCode.Expires < DateTime.UtcNow)
//         {
//             // TODO
//             throw new Exception("Invalid or expired authorization code.");
//         }
//
//         // Create a code challenge (hash) from the given code verifier.
//         // This should match the previously received code challenge in the database.
//         var computedCodeChallenge = ComputeCodeChallenge(command.CodeVerifier);
//         if (computedCodeChallenge != authorizationCode.CodeChallenge)
//         {
//             // TODO
//             throw new Exception("Invalid code verifier.");
//         }
//
//         // Authorization successful, remove authorization code.
//         context.AuthorizationCodes.Remove(authorizationCode);
//
//         // Create tokens.
//         var claims = new List<Claim>
//         {
//             new (ClaimTypes.NameIdentifier, authorizationCode.UserId.ToString())
//         };
//         //
//         var accessToken = tokenService.GenerateAccessToken(claims, DateTime.UtcNow.AddMinutes(15));
//         var refreshToken = tokenService.GenerateRefreshToken();
//
//         // Store the refresh token.
//         // TODO Expiration?
//         // TODO Add UserId
// //        context.RefreshTokens.Add(new RefreshToken { Token = refreshToken });
//
//         return new GetUserTokenResult(accessToken, refreshToken);
//
//         // // Store refresh token in HTTP-only cookie
//         // http.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
//         // {
//         //     HttpOnly = true,
//         //     Secure = true,
//         //     SameSite = SameSiteMode.Strict,
//         //     Expires = DateTime.UtcNow.AddDays(7)
//         // });
//     }
//
//     private static string ComputeCodeChallenge(string codeVerifier)
//     {
//         var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
//         return Convert.ToBase64String(hash)
//             .Replace("+", "-")
//             .Replace("/", "_")
//             .Replace("=", ""); // Base64 URL encoding
//     }
// }