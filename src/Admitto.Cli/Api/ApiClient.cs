using System.Net.Http.Headers;

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.Converters.Add(new Iso8601TimeSpanConverter());
    }
    
    public void SetBearerToken(string token)  
    {  
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);  
    }  
}
