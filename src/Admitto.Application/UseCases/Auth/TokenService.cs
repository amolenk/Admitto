// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text;
// using Microsoft.Extensions.Configuration;
// using Microsoft.IdentityModel.Tokens;
//
// namespace Amolenk.Admitto.Application.UseCases.Auth
// {
//     public class TokenService(IConfiguration configuration, IAuthContext context)
//     {
//         public string GenerateAccessToken(Guid userId, string email, List<Claim> additionalClaims = null)
//         {
//             var claims = new List<Claim>
//             {
//                 new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
//                 new Claim(JwtRegisteredClaimNames.Email, email),
//                 new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
//             };
//
//             if (additionalClaims != null)
//             {
//                 claims.AddRange(additionalClaims);
//             }
//
//             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));
//             var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
//
//             var token = new JwtSecurityToken(
//                 issuer: _config["Jwt:Issuer"],
//                 audience: _config["Jwt:Audience"],
//                 claims: claims,
//                 expires: DateTime.UtcNow.AddMinutes(15),
//                 signingCredentials: creds
//             );
//
//             return new JwtSecurityTokenHandler().WriteToken(token);
//         }
//
//         private string GenerateApplicationToken(Guid eventId)
//         {
//             var claims = new List<Claim>
//             {
//                 new Claim("event_id", eventId.ToString()),
//                 new Claim("role", "api_client")
//             };
//
//             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));
//             var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
//
//             var token = new JwtSecurityToken(
//                 issuer: _config["Jwt:Issuer"],
//                 audience: _config["Jwt:Audience"],
//                 claims: claims,
//                 expires: DateTime.UtcNow.AddMinutes(30),
//                 signingCredentials: creds
//             );
//
//             return new JwtSecurityTokenHandler().WriteToken(token);
//         }
//         
//         private (string AccessToken, string RefreshToken) GenerateUserTokens(object user)
//         {
//             var claims = new List<Claim>
//             {
//                 new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
//                 new Claim(ClaimTypes.Email, user.Email)
//             };
//
//             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));
//             var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
//
//             var accessToken = new JwtSecurityToken(
//                 issuer: _config["Jwt:Issuer"],
//                 audience: _config["Jwt:Audience"],
//                 claims: claims,
//                 expires: DateTime.UtcNow.AddMinutes(15),
//                 signingCredentials: creds
//             );
//
//             var refreshToken = Guid.NewGuid().ToString();
//             context.RefreshTokens.StoreRefreshToken(user.Id, refreshToken);
//
//             return (new JwtSecurityTokenHandler().WriteToken(accessToken), refreshToken);
//         }
//
//         public string GenerateAccessToken(IEnumerable<Claim> claims, DateTime? expires)
//         {
//             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));
//             var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
//
//             var accessToken = new JwtSecurityToken(
//                 issuer: _config["Jwt:Issuer"],
//                 audience: _config["Jwt:Audience"],
//                 claims: claims,
//                 expires: expires,
//                 signingCredentials: creds
//             );
//
//             return new JwtSecurityTokenHandler().WriteToken(accessToken);
//         }
//
//         public string GenerateRefreshToken()
//         {
//             var refreshToken = Guid.NewGuid().ToString();
//             
//             return refreshToken;
//         }
//         
//     }
// }