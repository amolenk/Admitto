using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Sending.Settings;

[TestClass]
public sealed class EffectiveEmailSettingsResolverTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async Task ResolveAsync_ValidSystemConfiguration_ReturnsEffectiveSettings()
    {
        var teamId = TeamId.New();
        var resolver = new EffectiveEmailSettingsResolver(Options.Create(new SystemEmailOptions
        {
            SmtpHost = "smtp.admitto.org",
            SmtpPort = 587,
            SmtpStartTls = true,
            FromAddress = "tickets@admitto.org",
            AuthMode = nameof(EmailAuthMode.Basic),
            Username = "smtp-user",
            Password = "smtp-password"
        }), Environment.EmailDatabase.Context);

        var result = await resolver.ResolveAsync(teamId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.SmtpHost.Value.ShouldBe("smtp.admitto.org");
        result.SmtpPort.Value.ShouldBe(587);
        result.SmtpSsl.ShouldBeFalse();
        result.SmtpStartTls.ShouldBeTrue();
        result.FromAddress.Value.ShouldBe("tickets@admitto.org");
        result.FromDisplayName.ShouldBe("tickets@admitto.org");
        result.ReplyToAddress.ShouldBeNull();
        result.AuthMode.ShouldBe(EmailAuthMode.Basic);
        result.Username.ShouldBe("smtp-user");
        result.Password.ShouldBe("smtp-password");
    }

    [TestMethod]
    public async Task ResolveAsync_ProjectedTeamContext_ReturnsTeamNameDisplayNameAndReplyTo()
    {
        var teamId = TeamId.New();
        await Environment.EmailDatabase.SeedAsync(dbContext =>
        {
            var view = TeamEmailContextView.CreatePartial(teamId, DateTimeOffset.UtcNow);
            view.UpdateTeamContext("Acme", "#0f766e", "help@example.com", teamVersion: 1, DateTimeOffset.UtcNow);
            dbContext.TeamEmailContexts.Add(view);
        });

        var resolver = new EffectiveEmailSettingsResolver(Options.Create(new SystemEmailOptions
        {
            SmtpHost = "smtp.admitto.org",
            SmtpPort = 587,
            FromAddress = "tickets@admitto.org"
        }), Environment.EmailDatabase.Context);

        var result = await resolver.ResolveAsync(teamId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.FromDisplayName.ShouldBe("Acme");
        result.ReplyToAddress.ShouldBe(EmailAddress.From("help@example.com"));
    }

    [TestMethod]
    public async Task ResolveAsync_MissingProjectedTeamName_ReturnsSystemFromAddressDisplayName()
    {
        var teamId = TeamId.New();
        await Environment.EmailDatabase.SeedAsync(dbContext =>
        {
            dbContext.TeamEmailContexts.Add(TeamEmailContextView.CreatePartial(teamId, DateTimeOffset.UtcNow));
        });

        var resolver = new EffectiveEmailSettingsResolver(Options.Create(new SystemEmailOptions
        {
            SmtpHost = "smtp.admitto.org",
            SmtpPort = 587,
            FromAddress = "tickets@admitto.org"
        }), Environment.EmailDatabase.Context);

        var result = await resolver.ResolveAsync(teamId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.FromDisplayName.ShouldBe("tickets@admitto.org");
        result.ReplyToAddress.ShouldBeNull();
    }

    [TestMethod]
    public async Task ResolveAsync_BooleanAuthConfiguration_ReturnsBasicAuthSettings()
    {
        var resolver = new EffectiveEmailSettingsResolver(Options.Create(new SystemEmailOptions
        {
            SmtpHost = "smtp.admitto.org",
            SmtpPort = 587,
            FromAddress = "tickets@admitto.org",
            AuthMode = "true",
            Username = "smtp-user",
            Password = "smtp-password"
        }), Environment.EmailDatabase.Context);

        var result = await resolver.ResolveAsync(TeamId.New(), testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.AuthMode.ShouldBe(EmailAuthMode.Basic);
        result.Username.ShouldBe("smtp-user");
        result.Password.ShouldBe("smtp-password");
    }

    [TestMethod]
    public async Task ResolveAsync_MissingRequiredConfiguration_ReturnsNull()
    {
        var resolver = new EffectiveEmailSettingsResolver(
            Options.Create(new SystemEmailOptions()),
            Environment.EmailDatabase.Context);

        var result = await resolver.ResolveAsync(TeamId.New(), testContext.CancellationToken);

        result.ShouldBeNull();
    }
}
