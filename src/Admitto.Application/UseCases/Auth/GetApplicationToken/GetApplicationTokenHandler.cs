// using Amolenk.Admitto.Application.UseCases.Accounts.Services;
//
// namespace Amolenk.Admitto.Application.UseCases.Accounts.GetApplicationToken;
//
// /// <summary>
// /// Get a JWT access token for the OAuth 2.0 client credentials flow.
// /// </summary>
// public class GetApplicationTokenHandler(IApplicationContext context, TokenService tokenService)
//     : ICommandHandler<GetApplicationTokenCommand>
// {
//     public ValueTask HandleAsync(GetApplicationTokenCommand command, CancellationToken cancellationToken)
//     {
//         // Case 1: API Client Authentication (Client Credentials)
//         if (!string.IsNullOrEmpty(command.ClientId) && !string.IsNullOrEmpty(command.ClientSecret))
//         {
//             var apiClient = await apiClientService.ValidateClientCredentials(command.ClientId, command.ClientSecret);
//             if (apiClient == null) return Results.Unauthorized();
//
//             var accessToken = tokenService.GenerateApiAccessToken(apiClient.EventId);
//             return Results.Ok(new { accessToken });
//         }
//         
//         return Results.BadRequest("Invalid request");
//     }
//     
//
// }