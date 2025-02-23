using System.Net;
using Amolenk.Admitto.Infrastructure.Persistence;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Application.Tests;

public class IntegrationTest1
{
    [Test]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var (app, httpClient) = await StartAppAsync();
        
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        if (applicationModel.Resources.FirstOrDefault(r => r.Name == "cosmos-db") is not ParameterResource
            {
                IsConnectionString: true
            } cosmosResource)
        {
            throw new Exception("Nope!");
        }
        
        var connectionString = cosmosResource.Value;
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseCosmos(connectionString!, "Orders", cosmosOptions =>
            {
                cosmosOptions.ConnectionMode(ConnectionMode.Gateway);
                cosmosOptions.LimitToEndpoint();
            })
            .Options;

        var testDbContext = new ApplicationDbContext(options);

        await testDbContext.Database.EnsureDeletedAsync();
        await testDbContext.Database.EnsureCreatedAsync();
        
        testDbContext.Orders.Add(new Order()
        {
            Id = 1,
            PartitionKey = "1",
            ShippingAddress = new StreetAddress()
            {
                Street = "123 Main St",
                City = "Anytown"
            }
        });
        await testDbContext.SaveChangesAsync();
        
        
        // Act
        var response = await httpClient.GetAsync("/registrations");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        await app.DisposeAsync();
    }

    private static async Task<(DistributedApplication, HttpClient)> StartAppAsync()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Admitto_AppHost>();
        
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        var app = await appHost.BuildAsync();

        var resourceNotificationService = app.Services
            .GetRequiredService<ResourceNotificationService>();

        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("api");

        await resourceNotificationService.WaitForResourceAsync(
                "api",
                KnownResourceStates.Running
            )
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        return (app, httpClient);
    }
}