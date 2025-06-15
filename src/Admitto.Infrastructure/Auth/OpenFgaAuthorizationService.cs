using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.ValueObjects;
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

    public async ValueTask AddTeamRoleAsync(Guid userId, TeamId teamId, TeamMemberRole role,
        CancellationToken cancellationToken = default)
    {
        // Don't add the same tuple if it already exists
        if (await TupleExistsAsync(userId, role.Value, "team", teamId.Value.ToString(), cancellationToken))
        {
            return;
        }
        
        var request = new ClientWriteRequest(
            [
                new ClientTupleKey
                {
                    User = $"user:{userId}",
                    Relation = role.Value.ToLowerInvariant(),
                    Object = $"team:{teamId.Value}"
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

    public async ValueTask<IEnumerable<TeamMemberRole>> GetTeamRolesAsync(Guid userId, TeamId teamId, 
        CancellationToken cancellationToken = default)
    {
        var request = new ClientReadRequest {
            User = $"user:{userId}",
            Object = $"team:{teamId.Value}"
        };

        var client = await clientFactory.GetClientAsync();
        var response = await client.Read(request, cancellationToken: cancellationToken);

        return response.Tuples.Select(t => new TeamMemberRole(t.Key.Relation));
    }
    
    private async ValueTask<bool> TupleExistsAsync(Guid userId, string relation, string objectType, string objectId, 
        CancellationToken cancellationToken = default)
    {
        var request = new ClientReadRequest {
            User = $"user:{userId}",
            Relation = relation,
            Object = $"{objectType}:{objectId}"
        };

        var client = await clientFactory.GetClientAsync();
        var response = await client.Read(request, cancellationToken: cancellationToken);

        return response.Tuples.Count != 0;
    }
}
