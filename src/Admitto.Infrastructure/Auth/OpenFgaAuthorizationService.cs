using Amolenk.Admitto.Application.Common.Abstractions;
using OpenFga.Sdk.Client.Model;

namespace Amolenk.Admitto.Infrastructure.Auth;

public class OpenFgaAuthorizationService(OpenFgaClientFactory clientFactory) : IRebacAuthorizationService
{
    public async ValueTask AddGlobalAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var request = new ClientWriteRequest(
            [
                new ClientTupleKey
                {
                    User = $"user:{userId}",
                    Relation = "admin",
                    Object = "system:system"
                }
            ],
            []
        );
    
        var client = await clientFactory.GetClientAsync();
        await client.Write(request, cancellationToken: cancellationToken);
    }

    public async ValueTask<bool> CheckAsync(Guid userId, string relation, string objectType, string objectId,
        CancellationToken cancellationToken = default)
    {
        var request = new ClientCheckRequest {
            User = $"user:{userId}",
            Relation = relation,
            Object = $"{objectType}:{objectId}"
        };

        var client = await clientFactory.GetClientAsync();
        var response = await client.Check(request, cancellationToken: cancellationToken);

        return response.Allowed ?? false;
    }
}
