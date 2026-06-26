using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.EmailSettings.SendTestEmail;

[TestClass]
public sealed class SendTestEmailTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SendTestEmail_TeamScope_SendsDiagnosticWithoutEmailLog()
    {
        var fixture = SendTestEmailFixture.TeamSettings();
        await fixture.SetupAsync(Environment, testContext.CancellationToken);
        var sut = fixture.CreateHandler(Environment);

        await sut.HandleAsync(fixture.TeamCommand("ops@acme.org"), testContext.CancellationToken);

        fixture.EmailSender.SentMessages.Count.ShouldBe(1);
        var sent = fixture.EmailSender.SentMessages.Single();
        sent.Message.RecipientAddress.ShouldBe("ops@acme.org");
        sent.Settings.FromAddress.Value.ShouldBe("team@example.com");

        var logCount = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .CountAsync(testContext.CancellationToken);
        logCount.ShouldBe(0);
    }

    [TestMethod]
    public async ValueTask SendTestEmail_EventCommand_UsesTeamSettings()
    {
        var fixture = SendTestEmailFixture.EventAndTeamSettings();
        await fixture.SetupAsync(Environment, testContext.CancellationToken);
        var sut = fixture.CreateHandler(Environment);

        await sut.HandleAsync(fixture.EventCommand("ops@acme.org"), testContext.CancellationToken);

        fixture.EmailSender.SentMessages.Count.ShouldBe(1);
        var sent = fixture.EmailSender.SentMessages.Single();
        sent.Settings.SmtpHost.Value.ShouldBe("team.smtp.example.com");
        sent.Settings.FromAddress.Value.ShouldBe("team@example.com");
        sent.Settings.Password.ShouldBeNull();
    }

    [TestMethod]
    public async ValueTask SendTestEmail_IncompleteSavedSettings_DoesNotAttemptSmtp()
    {
        var fixture = SendTestEmailFixture.IncompleteEventSettings();
        await fixture.SetupAsync(Environment, testContext.CancellationToken);
        var sut = fixture.CreateHandler(Environment);

        var result = await ErrorResult.CaptureAsync(
            () => sut.HandleAsync(fixture.EventCommand(), testContext.CancellationToken));

        result.Error.ShouldMatch(SendTestEmailHandler.Errors.IncompleteSettings);
        fixture.EmailSender.SentMessages.ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask SendTestEmail_SmtpTransportFails_SurfacesTransportError()
    {
        var fixture = SendTestEmailFixture.TeamSettings();
        fixture.EmailSender.ExceptionMessage = "Authentication failed";
        await fixture.SetupAsync(Environment, testContext.CancellationToken);
        var sut = fixture.CreateHandler(Environment);

        var result = await ErrorResult.CaptureAsync(
            () => sut.HandleAsync(fixture.TeamCommand(), testContext.CancellationToken));

        result.Error.ShouldMatch(SendTestEmailHandler.Errors.SendFailed("Authentication failed"));

        var logCount = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .CountAsync(testContext.CancellationToken);
        logCount.ShouldBe(0);
    }
}
