using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.Converters.Add(new Iso8601TimeSpanConverter());
        settings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }
    
    public void SetBearerToken(string token)  
    {  
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);  
    }  
}
