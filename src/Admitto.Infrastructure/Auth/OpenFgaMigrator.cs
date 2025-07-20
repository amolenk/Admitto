using System.Text.Json;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

namespace Amolenk.Admitto.Infrastructure.Auth;

public class OpenFgaMigrator(OpenFgaClientFactory clientFactory)
{
    public async ValueTask MigrateAsync(Guid? adminUserId = null)
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
         
            // Configure global admin user.
            if (adminUserId is not null)
            {
                var authorizationService = new OpenFgaAuthorizationService(clientFactory);
                await authorizationService.AddGlobalAdminAsync(adminUserId.Value);
            }
        }
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
        var assembly = typeof(OpenFgaMigrator).Assembly;
        const string resourceName = "Amolenk.Admitto.Infrastructure.Auth.OpenFgaAuthorizationModel.json";

        await using var stream = assembly.GetManifestResourceStream(resourceName)
                                 ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}