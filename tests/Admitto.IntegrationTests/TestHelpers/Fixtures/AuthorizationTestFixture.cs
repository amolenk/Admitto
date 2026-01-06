// using Amolenk.Admitto.Application.Common.Abstractions;
// using Amolenk.Admitto.Infrastructure.Auth.OpenFga;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
//
// namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures;
//
// public class AuthorizationTestFixture
// {
//     private AuthorizationTestFixture(IAuthorizationService authorizationService)
//     {
//         AuthorizationService = authorizationService;
//     }
//
//     public IAuthorizationService AuthorizationService { get; }
//
//     public static AuthorizationTestFixture Create(TestingAspireAppHost appHost)
//     {
//         var loggerFactory = appHost.Application.Services.GetRequiredService<ILoggerFactory>();
//         
//         var httpClient = appHost.CreateHttpClient("openfga", "http");
//
//         var clientFactory = new OpenFgaClientFactory(
//             httpClient,
//             appHost.Application.Services.GetRequiredService<IConfiguration>(),
//             loggerFactory.CreateLogger<OpenFgaClientFactory>());
//         
//         var authorizationService = new OpenFgaAuthorizationService(
//             clientFactory,
//             loggerFactory.CreateLogger<OpenFgaAuthorizationService>());
//
//         return new AuthorizationTestFixture(authorizationService);
//     }
//
//     public async Task ResetAsync(Func<IAuthorizationService, ValueTask>? seed = null)
//     {
//         try
//         {
//             // TODO Delete all tuples
//
//             // Seed the authorization service with test data
//             if (seed is not null)
//             {
//                 await seed(AuthorizationService);
//             }
//         }
//         catch (Exception ex)
//         {
//             throw new InvalidOperationException("Failed to reset authorization test data", ex);
//         }
//     }
// }