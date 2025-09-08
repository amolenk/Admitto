using OpenFga.Sdk.Client;

namespace Amolenk.Admitto.Infrastructure.Auth.OpenFga;

public static class OpenFgaClientExtensions
{
    public static async ValueTask<string?> TryGetStoreIdAsync(this OpenFgaClient client)
    {
        var response = await client.ListStores();
        return response.Stores.FirstOrDefault(s => s.Name == OpenFgaClientFactory.StoreName)?.Id;
    }

    public static async ValueTask<string?> TryGetAuthorizationModelIdAsync(this OpenFgaClient client)
    {
        var response = await client.ReadLatestAuthorizationModel();
        return response?.AuthorizationModel?.Id;
    }
}