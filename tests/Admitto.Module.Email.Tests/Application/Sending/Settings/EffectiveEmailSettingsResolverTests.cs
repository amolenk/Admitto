using Amolenk.Admitto.Module.Email.Application.Settings;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.Settings;

[TestClass]
public sealed class EffectiveEmailSettingsResolverTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask ResolveAsync_EventScopedOnly_ReturnsEventSettings()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var protectedSecret = TestProtectedSecretFactory.Create();

        var settings = new EventEmailSettingsBuilder()
            .ForEvent(eventId)
            .WithSmtpHost("event-smtp.example.com")
            .WithBasicAuth(protectedPassword: protectedSecret.Protect("event-pass"))
            .Build();
        await Environment.Database.SeedAsync(db => db.EmailSettings.Add(settings));

        var resolver = new EffectiveEmailSettingsResolver(Environment.Database.Context, protectedSecret);
        var result = await resolver.ResolveAsync(teamId, eventId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.SmtpHost.Value.ShouldBe("event-smtp.example.com");
        result.Password.ShouldBe("event-pass");
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
        await Environment.Database.SeedAsync(db => db.EmailSettings.Add(settings));

        var resolver = new EffectiveEmailSettingsResolver(Environment.Database.Context, protectedSecret);
        var result = await resolver.ResolveAsync(teamId, eventId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.SmtpHost.Value.ShouldBe("team-smtp.example.com");
    }

    [TestMethod]
    public async ValueTask ResolveAsync_BothPresent_EventWins()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var protectedSecret = TestProtectedSecretFactory.Create();

        var eventSettings = new EventEmailSettingsBuilder()
            .ForEvent(eventId)
            .WithSmtpHost("event-smtp.example.com")
            .Build();
        var teamSettings = new EventEmailSettingsBuilder()
            .ForTeam(teamId)
            .WithSmtpHost("team-smtp.example.com")
            .Build();
        await Environment.Database.SeedAsync(db =>
        {
            db.EmailSettings.Add(eventSettings);
            db.EmailSettings.Add(teamSettings);
        });

        var resolver = new EffectiveEmailSettingsResolver(Environment.Database.Context, protectedSecret);
        var result = await resolver.ResolveAsync(teamId, eventId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.SmtpHost.Value.ShouldBe("event-smtp.example.com");
    }

    [TestMethod]
    public async ValueTask ResolveAsync_NeitherPresent_ReturnsNull()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var protectedSecret = TestProtectedSecretFactory.Create();

        var resolver = new EffectiveEmailSettingsResolver(Environment.Database.Context, protectedSecret);
        var result = await resolver.ResolveAsync(teamId, eventId, testContext.CancellationToken);

        result.ShouldBeNull();
    }

    [TestMethod]
    public async ValueTask ResolveAsync_EventSettingsPresent_ReturnedRegardlessOfValidity()
    {
        // The resolver does NOT skip invalid settings — the caller decides what to do.
        // Event-scoped settings always win over team-scoped when present.
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var protectedSecret = TestProtectedSecretFactory.Create();

        var eventSettings = new EventEmailSettingsBuilder()
            .ForEvent(eventId)
            .WithSmtpHost("event-smtp.example.com")
            .Build();
        var teamSettings = new EventEmailSettingsBuilder()
            .ForTeam(teamId)
            .WithSmtpHost("team-smtp.example.com")
            .Build();
        await Environment.Database.SeedAsync(db =>
        {
            db.EmailSettings.Add(eventSettings);
            db.EmailSettings.Add(teamSettings);
        });

        var resolver = new EffectiveEmailSettingsResolver(Environment.Database.Context, protectedSecret);
        var result = await resolver.ResolveAsync(teamId, eventId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.SmtpHost.Value.ShouldBe("event-smtp.example.com");
    }
}
