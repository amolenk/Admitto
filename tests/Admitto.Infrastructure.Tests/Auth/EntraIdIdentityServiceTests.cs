using Amolenk.Admitto.Infrastructure.Auth;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using NSubstitute;

namespace Amolenk.Admitto.Infrastructure.Tests.Auth;

[TestClass]
public class EntraIdIdentityServiceTests
{
    private GraphServiceClient _mockGraphServiceClient = null!;
    private EntraIdIdentityService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockGraphServiceClient = Substitute.For<GraphServiceClient>();
        _service = new EntraIdIdentityService(_mockGraphServiceClient);
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

        _mockGraphServiceClient.Users
            .GetAsync(Arg.Any<Action<UsersRequestBuilder.UsersRequestBuilderGetRequestConfiguration>>(), default)
            .Returns(mockResponse);

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

        _mockGraphServiceClient.Users
            .GetAsync(Arg.Any<Action<UsersRequestBuilder.UsersRequestBuilderGetRequestConfiguration>>(), default)
            .Returns(mockResponse);

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

        _mockGraphServiceClient.Users
            .GetAsync(Arg.Any<Action<UsersRequestBuilder.UsersRequestBuilderGetRequestConfiguration>>(), default)
            .Returns(mockResponse);

        // Act
        var result = await _service.GetUsersAsync();

        // Assert
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task AddUserAsync_InvitesGuestUser_ReturnsUser()
    {
        // Arrange
        var email = "newuser@example.com";
        var userId = Guid.NewGuid().ToString();
        var createdInvitation = new Invitation
        {
            InvitedUser = new Microsoft.Graph.Models.User
            {
                Id = userId,
                Mail = email
            }
        };

        _mockGraphServiceClient.Invitations
            .PostAsync(Arg.Any<Invitation>(), default)
            .Returns(createdInvitation);

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
        await _mockGraphServiceClient.Users[userId.ToString()].Received(1).DeleteAsync(default);
    }
}