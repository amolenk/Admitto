using Amolenk.Admitto.Infrastructure.Auth.OpenFga;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Migrators;

public class OpenFgaMigrator(OpenFgaClientFactory clientFactory, ILogger<OpenFgaAuthorizationService> logger) : IMigrator
{
    public async ValueTask RunAsync(CancellationToken cancellationToken)
    {
        var authorizationService = new OpenFgaAuthorizationService(clientFactory, logger);
        await authorizationService.MigrateAsync();
    }
}
