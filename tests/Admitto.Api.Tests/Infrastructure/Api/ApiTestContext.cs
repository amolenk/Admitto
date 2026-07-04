using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Api.Tests.Infrastructure.Api;

public class ApiTestContext
{
    public HttpClient Client { get; }
    
    private ApiTestContext(HttpClient client)
    {
        Client = client;
    }

    public static ApiTestContext Create(DistributedApplication application)
    {
        var httpClient = application.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AdmittoApi");
        
        // httpClient.BaseAddress = application.GetEndpoint("api");
        
        return new ApiTestContext(httpClient);
    }
}