using Amolenk.Admitto.Infrastructure.Auth;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Moq;

namespace Amolenk.Admitto.Infrastructure.Tests.Auth;

[TestClass]
public class EntraIdIdentityServiceTests
{
    private Mock<GraphServiceClient> _mockGraphServiceClient = null!;
    private EntraIdIdentityService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockGraphServiceClient = new Mock<GraphServiceClient>();
        _service = new EntraIdIdentityService(_mockGraphServiceClient.Object);
    }

    [TestMethod]
    public async Task GetUserByEmailAsync_UserExists_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";
        var userId = Guid.NewGuid().ToString();
        var mockUser = new Microsoft.Graph.Models.User
        {
            Id = userId,
            Mail = email
        };

        var mockResponse = new UserCollectionResponse
        {
            Value = new List<Microsoft.Graph.Models.User> { mockUser }
        };

        _mockGraphServiceClient
            .Setup(x => x.Users.GetAsync(It.IsAny<Action<UsersRequestBuilder.UsersRequestBuilderGetRequestConfiguration>>(), default))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.GetUserByEmailAsync(email);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(Guid.Parse(userId), result.Id);
        Assert.AreEqual(email, result.Email);
    }

    [TestMethod]
    public async Task GetUserByEmailAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var mockResponse = new UserCollectionResponse
        {
            Value = new List<Microsoft.Graph.Models.User>()
        };

        _mockGraphServiceClient
            .Setup(x => x.Users.GetAsync(It.IsAny<Action<UsersRequestBuilder.UsersRequestBuilderGetRequestConfiguration>>(), default))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.GetUserByEmailAsync(email);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetUsersAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<Microsoft.Graph.Models.User>
        {
            new() { Id = Guid.NewGuid().ToString(), Mail = "user1@example.com" },
            new() { Id = Guid.NewGuid().ToString(), Mail = "user2@example.com" }
        };

        var mockResponse = new UserCollectionResponse { Value = users };

        _mockGraphServiceClient
            .Setup(x => x.Users.GetAsync(It.IsAny<Action<UsersRequestBuilder.UsersRequestBuilderGetRequestConfiguration>>(), default))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.GetUsersAsync();

        // Assert
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task AddUserAsync_CreatesUser_ReturnsUser()
    {
        // Arrange
        var email = "newuser@example.com";
        var userId = Guid.NewGuid().ToString();
        var createdUser = new Microsoft.Graph.Models.User
        {
            Id = userId,
            Mail = email
        };

        _mockGraphServiceClient
            .Setup(x => x.Users.PostAsync(It.IsAny<Microsoft.Graph.Models.User>(), default))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _service.AddUserAsync(email);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(Guid.Parse(userId), result.Id);
        Assert.AreEqual(email, result.Email);
    }

    [TestMethod]
    public async Task DeleteUserAsync_CallsGraphApi()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.DeleteUserAsync(userId);

        // Assert
        _mockGraphServiceClient.Verify(
            x => x.Users[userId.ToString()].DeleteAsync(default),
            Times.Once);
    }
}