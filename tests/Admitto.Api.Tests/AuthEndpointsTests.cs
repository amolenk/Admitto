namespace Amolenk.Admitto.Application.Tests;

[TestClass]
public class AuthEndpointsTests : DistributedAppTestBase
{
    [TestMethod]
    public async Task SendMagicLink_UserExists_SendsMagicLinkViaEmail()
    {
        // Arrange
        const string email = "alice@example.com";
        const string codeChallenge = "code_challenge";
        
        // Act
        var response = await Api.GetAsync($"/authorize?email={email}&codeChallenge={codeChallenge}");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task SendMagicLink_UserDoesNotExist_DoesNotSendMagicLink()
    {
        const string email = "chuck@example.com";
        const string codeChallenge = "code_challenge";
        
        // Act
        var response = await Api.GetAsync($"/authorize?email={email}&codeChallenge={codeChallenge}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}