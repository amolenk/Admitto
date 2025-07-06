using Amolenk.Admitto.Cli.Services;
using Amolenk.Admitto.Cli.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Admitto.Cli.Tests;

[TestClass]
public sealed class ApiServiceTests
{
    private Mock<IConfiguration> _mockConfiguration = null!;
    private ApiService _apiService = null!;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.SetupGet(c => c["Api:BaseUrl"]).Returns("https://localhost:5001/api/");
        _mockConfiguration.SetupGet(c => c["Auth:Token"]).Returns((string?)null);

        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:5001/api/")
        };

        var mockAuthService = new Mock<IAuthService>();
        mockAuthService.Setup(x => x.GetAccessTokenAsync()).ReturnsAsync("mock-access-token");

        _apiService = new ApiService(_httpClient, _mockConfiguration.Object, mockAuthService.Object);
    }

    [TestMethod]
    public async Task PostAsync_ShouldReturnSuccess_WhenApiReturnsSuccessResponse()
    {
        // Arrange
        var teamRequest = new CreateTeamRequest(
            "Test Team",
            new EmailSettingsDto("test@example.com", "smtp.example.com", 587),
            new List<TeamMemberDto>()
        );

        var expectedResponse = new CreateTeamResponse(Guid.NewGuid());
        var jsonResponse = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _apiService.PostAsync<CreateTeamResponse>("teams", teamRequest);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(expectedResponse.Id, result.Data.Id);
    }

    [TestMethod]
    public async Task PostAsync_ShouldReturnValidationErrors_WhenApiReturnsValidationError()
    {
        // Arrange
        var teamRequest = new CreateTeamRequest(
            "", // Invalid empty name
            new EmailSettingsDto("test@example.com", "smtp.example.com", 587),
            new List<TeamMemberDto>()
        );

        var validationErrors = new Dictionary<string, string[]>
        {
            ["name"] = ["Team name is required.", "Team name must be at least 2 characters long."]
        };

        var problemDetails = new
        {
            title = "Validation failed",
            status = 400,
            errors = validationErrors
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _apiService.PostAsync<CreateTeamResponse>("teams", teamRequest);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Validation failed", result.Error);
        Assert.AreEqual(400, result.StatusCode);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("name"));
        Assert.AreEqual(2, result.ValidationErrors["name"].Length);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _httpClient?.Dispose();
        _mockHttpMessageHandler?.Object?.Dispose();
    }
}
