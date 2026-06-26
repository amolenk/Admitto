using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Email.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Sending.Settings;

[TestClass]
public sealed class EffectiveEmailSettingsResolverTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask ResolveAsync_TeamSettings_ReturnsTeamSettingsForEvent()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var protectedSecret = TestProtectedSecretFactory.Create();

        var settings = new EventEmailSettingsBuilder()
            .ForTeamAndEvent(teamId, eventId)
            .WithSmtpHost("team-smtp.example.com")
            .WithBasicAuth(protectedPassword: protectedSecret.Protect("team-pass"))
            .Build();
        await Environment.EmailDatabase.SeedAsync(db => db.EmailSettings.Add(settings));

        var resolver = new EffectiveEmailSettingsResolver(Environment.EmailDatabase.Context, protectedSecret);
        var result = await resolver.ResolveAsync(teamId, eventId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.SmtpHost.Value.ShouldBe("team-smtp.example.com");
        result.Password.ShouldBe("team-pass");
    }

    [TestMethod]
    public async ValueTask ResolveAsync_TeamScopedOnly_ReturnsTeamSettings()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var protectedSecret = TestProtectedSecretFactory.Create();

        var settings = new EventEmailSettingsBuilder()
            .ForTeam(teamId)
            .WithSmtpHost("team-smtp.example.com")
            .Build();
        await Environment.EmailDatabase.SeedAsync(db => db.EmailSettings.Add(settings));

        var resolver = new EffectiveEmailSettingsResolver(Environment.EmailDatabase.Context, protectedSecret);
        var result = await resolver.ResolveAsync(teamId, eventId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.SmtpHost.Value.ShouldBe("team-smtp.example.com");
    }

    [TestMethod]
    public async ValueTask ResolveAsync_NeitherPresent_ReturnsNull()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var protectedSecret = TestProtectedSecretFactory.Create();

        var resolver = new EffectiveEmailSettingsResolver(Environment.EmailDatabase.Context, protectedSecret);
        var result = await resolver.ResolveAsync(teamId, eventId, testContext.CancellationToken);

        result.ShouldBeNull();
    }

    [TestMethod]
    public async ValueTask ResolveAsync_SettingsForDifferentTeam_ReturnsNull()
    {
        var teamId = TeamId.New();
        var otherTeamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var protectedSecret = TestProtectedSecretFactory.Create();

        var settings = new EventEmailSettingsBuilder()
            .ForTeamAndEvent(otherTeamId, eventId)
            .WithSmtpHost("other-team-smtp.example.com")
            .Build();
        await Environment.EmailDatabase.SeedAsync(db => db.EmailSettings.Add(settings));

        var resolver = new EffectiveEmailSettingsResolver(Environment.EmailDatabase.Context, protectedSecret);
        var result = await resolver.ResolveAsync(teamId, eventId, testContext.CancellationToken);

        result.ShouldBeNull();
    }

}
