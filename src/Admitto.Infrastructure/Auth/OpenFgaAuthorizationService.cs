using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.ValueObjects;
using OpenFga.Sdk.Client.Model;

namespace Amolenk.Admitto.Infrastructure.Auth;

public class OpenFgaAuthorizationService(OpenFgaClientFactory clientFactory) : IAuthorizationService
{
    private const string SystemObject = "system:system";

    public ValueTask<bool> CanCreateTeamAsync(Guid userId, CancellationToken cancellationToken = default) =>
        CheckAsync(userId, "can_create_team", SystemObject, cancellationToken);

    public ValueTask<bool> CanUpdateTeamAsync(Guid userId, string teamSlug, 
        CancellationToken cancellationToken = default) =>
        CheckAsync(userId, "can_update_team", $"team:{teamSlug}", cancellationToken);
    
    public ValueTask<bool> CanViewTeamAsync(Guid userId, string teamSlug, 
        CancellationToken cancellationToken = default) =>
        CheckAsync(userId, "can_view_team", $"team:{teamSlug}", cancellationToken);

    public ValueTask<bool> CanCreateEventAsync(Guid userId, string teamSlug, 
        CancellationToken cancellationToken = default) =>
        CheckAsync(userId, "can_create_event", $"team:{teamSlug}", cancellationToken);

    public ValueTask<bool> CanUpdateEventAsync(Guid userId, string teamSlug, string eventSlug, 
        CancellationToken cancellationToken = default) =>
        CheckAsync(userId, "can_update_event", $"event:{teamSlug}_{eventSlug}", cancellationToken);

    public ValueTask<bool> CanViewEventAsync(Guid userId, string teamSlug, string eventSlug, 
        CancellationToken cancellationToken = default) =>
        CheckAsync(userId, "can_view_event", $"event:{teamSlug}_{eventSlug}", cancellationToken);

    public ValueTask AddGlobalAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
        AddTupleAsync($"user:{userId}", "admin", SystemObject, cancellationToken);

    public ValueTask AddTeamAsync(string teamSlug, CancellationToken cancellationToken = default) =>
        AddTupleAsync(SystemObject, "system", $"team:{teamSlug}", cancellationToken);

    public ValueTask AddTicketedEventAsync(string teamSlug, string eventSlug, 
        CancellationToken cancellationToken = default) =>
        AddTupleAsync($"team:{teamSlug}", "team", $"event:{teamSlug}_{eventSlug}", cancellationToken);

    public ValueTask AddTeamRoleAsync(Guid userId, string teamSlug, TeamMemberRole role, 
        CancellationToken cancellationToken = default) =>
        AddTupleAsync($"user:{userId}", role.Value.ToLowerInvariant(), $"team:{teamSlug}", 
            cancellationToken);

    public ValueTask<IEnumerable<string>> GetTeamsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        ListObjectsAsync($"user:{userId}", "can_view_team", "team", cancellationToken);

    public async ValueTask<IEnumerable<string>> GetTicketedEventsAsync(Guid userId, string teamSlug,
        CancellationToken cancellationToken = default)
    {
        var objectIds = await ListObjectsAsync($"user:{userId}", "can_view_event",
            "event", cancellationToken);

        return objectIds
            .Where(o => o.StartsWith(teamSlug))
            .Select(o => o[(teamSlug.Length + 1)..]); // Skip the team slug prefix
    }
    
    private async ValueTask<bool> CheckAsync(Guid userId, string relation, string obj,
        CancellationToken cancellationToken = default)
    {
        var request = new ClientCheckRequest {
            User = $"user:{userId}",
            Relation = relation,
            Object = obj
        };

        var client = await clientFactory.GetClientAsync();
        var response = await client.Check(request, cancellationToken: cancellationToken);

        return response.Allowed ?? false;
    }

    private async ValueTask AddTupleAsync(string user, string relation, string obj, 
        CancellationToken cancellationToken = default)
    {
        // Don't add the same tuple if it already exists
        if (await TupleExistsAsync(user, relation, obj, cancellationToken))
        {
            return;
        }
        
        var request = new ClientWriteRequest(
            [
                new ClientTupleKey
                {
                    User = user,
                    Relation = relation,
                    Object = obj
                }
            ],
            []
        );
    
        var client = await clientFactory.GetClientAsync();
        await client.Write(request, cancellationToken: cancellationToken);
    }
    
    private async ValueTask<IEnumerable<string>> ListObjectsAsync(string user, string relation, string type,
        CancellationToken cancellationToken = default)
    {
        var request = new ClientListObjectsRequest {
            User = user,
            Relation = relation,
            Type = type
        };
    
        var client = await clientFactory.GetClientAsync();
        var response = await client.ListObjects(request, cancellationToken: cancellationToken);
    
        return response.Objects.Select(obj => obj[(type.Length + 1)..]); // Skip the type prefix
    }
    
    private async ValueTask<bool> TupleExistsAsync(string user, string relation, string obj, 
        CancellationToken cancellationToken = default)
    {
        var request = new ClientReadRequest {
            User = user,
            Relation = relation,
            Object = obj
        };

        var client = await clientFactory.GetClientAsync();
        var response = await client.Read(request, cancellationToken: cancellationToken);

        return response.Tuples.Count != 0;
    }
}
