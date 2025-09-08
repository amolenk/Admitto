using System.Text.Json;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

namespace Amolenk.Admitto.Infrastructure.Auth.OpenFga;

public class OpenFgaAuthorizationService(OpenFgaClientFactory clientFactory, ILogger<OpenFgaAuthorizationService> logger)
    : IAuthorizationService
{
    public ValueTask<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(false);

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

    public ValueTask AddTicketedEventAsync(string teamSlug, string eventSlug, 
        CancellationToken cancellationToken = default) =>
        AddTupleAsync($"team:{teamSlug}", "team", $"event:{teamSlug}_{eventSlug}", cancellationToken);

    public ValueTask AddTeamRoleAsync(Guid userId, string teamSlug, TeamMemberRole role, 
        CancellationToken cancellationToken = default) =>
        AddTupleAsync($"user:{userId}", role.ToString().ToLowerInvariant(), $"team:{teamSlug}", 
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
    
    public async ValueTask MigrateAsync()
    {
        var client = await clientFactory.GetClientAsync();
        
        // Ensure the store exists and retrieve its ID
        var storeId = await client.TryGetStoreIdAsync() ?? await CreateStoreAsync(client);
        
        // Get a new client with the updated store ID
        client = await clientFactory.UpdateStoreIdAsync(storeId);
        
        // Ensure the authorization model exists
        var modelId = await client.TryGetAuthorizationModelIdAsync();
        if (modelId is null)
        {
            await WriteAuthorizationModelAsync(client);
        }
        
        logger.LogInformation("OpenFGA migration completed. Store ID: {StoreId}, Authorization Model ID: {ModelId}",
            storeId, modelId);
    }

    private static async ValueTask<string> CreateStoreAsync(OpenFgaClient client)
    {
        var response = await client.CreateStore(
            new ClientCreateStoreRequest { Name = OpenFgaClientFactory.StoreName });

        return response.Id;
    }
    
    private static async ValueTask<string> WriteAuthorizationModelAsync(OpenFgaClient client)
    {
        var modelJson = await LoadAuthorizationModelJsonAsync();
        var body = JsonSerializer.Deserialize<ClientWriteAuthorizationModelRequest>(modelJson)!;
    
        var response = await client.WriteAuthorizationModel(body);

        return response.AuthorizationModelId;
    }
    
    private static async ValueTask<string> LoadAuthorizationModelJsonAsync()
    {
        var assembly = typeof(OpenFgaAuthorizationService).Assembly;
        const string resourceName = "Amolenk.Admitto.Infrastructure.Auth.OpenFga.OpenFgaAuthorizationModel.json";

        await using var stream = assembly.GetManifestResourceStream(resourceName)
                                 ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
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
