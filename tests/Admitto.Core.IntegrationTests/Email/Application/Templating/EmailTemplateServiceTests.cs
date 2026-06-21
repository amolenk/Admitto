using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Email.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Templating;

[TestClass]
public sealed class EmailTemplateServiceTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask LoadAsync_EventScopedTemplate_ReturnsEventTemplate()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var template = new EmailTemplateBuilder()
            .ForTeamAndEvent(teamId, eventId)
            .WithSubject("Event subject")
            .Build();
        await Environment.EmailDatabase.SeedAsync(db => db.EmailTemplates.Add(template));

        var service = new EmailTemplateService(Environment.EmailDatabase.Context);
        var result = await service.LoadAsync(BuiltInEmailTemplateNames.TicketConfirmation, teamId, eventId, testContext.CancellationToken);

        result.Subject.ShouldBe("Event subject");
        result.TeamId.ShouldBe(teamId);
        result.TicketedEventId.ShouldBe(eventId);
    }

    [TestMethod]
    public async ValueTask LoadAsync_TeamScopedTemplate_ReturnsTeamTemplate()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var template = new EmailTemplateBuilder()
            .ForTeam(teamId)
            .WithSubject("Team subject")
            .Build();
        await Environment.EmailDatabase.SeedAsync(db => db.EmailTemplates.Add(template));

        var service = new EmailTemplateService(Environment.EmailDatabase.Context);
        var result = await service.LoadAsync(BuiltInEmailTemplateNames.TicketConfirmation, teamId, eventId, testContext.CancellationToken);

        result.Subject.ShouldBe("Team subject");
        result.TeamId.ShouldBe(teamId);
        result.TicketedEventId.ShouldBeNull();
    }

    [TestMethod]
    public async ValueTask LoadAsync_BothScopesPresent_ReturnsEventScopedTemplate()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var teamTemplate = new EmailTemplateBuilder()
            .ForTeam(teamId)
            .WithSubject("Team subject")
            .Build();
        var eventTemplate = new EmailTemplateBuilder()
            .ForTeamAndEvent(teamId, eventId)
            .WithSubject("Event subject")
            .Build();
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailTemplates.Add(teamTemplate);
            db.EmailTemplates.Add(eventTemplate);
        });

        var service = new EmailTemplateService(Environment.EmailDatabase.Context);
        var result = await service.LoadAsync(BuiltInEmailTemplateNames.TicketConfirmation, teamId, eventId, testContext.CancellationToken);

        result.Subject.ShouldBe("Event subject");
        result.TeamId.ShouldBe(teamId);
        result.TicketedEventId.ShouldBe(eventId);
    }

    [TestMethod]
    public async ValueTask LoadAsync_NoCustomTemplate_ReturnsBuiltInDefault()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var service = new EmailTemplateService(Environment.EmailDatabase.Context);
        var result = await service.LoadAsync(BuiltInEmailTemplateNames.TicketConfirmation, teamId, eventId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.TextBody.ShouldNotBeNullOrEmpty();
        result.HtmlBody.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public async ValueTask LoadAsync_EventTemplateForDifferentTeam_IgnoresTemplate()
    {
        var teamId = TeamId.New();
        var otherTeamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var template = new EmailTemplateBuilder()
            .ForTeamAndEvent(otherTeamId, eventId)
            .WithSubject("Other team subject")
            .Build();
        await Environment.EmailDatabase.SeedAsync(db => db.EmailTemplates.Add(template));

        var service = new EmailTemplateService(Environment.EmailDatabase.Context);
        var result = await service.LoadAsync(BuiltInEmailTemplateNames.TicketConfirmation, teamId, eventId, testContext.CancellationToken);

        result.Subject.ShouldNotBe("Other team subject");
    }

    [TestMethod]
    public async ValueTask LoadAsync_ReconfirmCancelledWithoutCustomTemplate_ReturnsBuiltInDefault()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var service = new EmailTemplateService(Environment.EmailDatabase.Context);
        var result = await service.LoadAsync(BuiltInEmailTemplateNames.ReconfirmCancelled, teamId, eventId, testContext.CancellationToken);

        result.Subject.ShouldContain("Cancelled");
        result.TextBody.ShouldContain("automatically cancelled");
        result.HtmlBody.ShouldNotBeNull();
        result.HtmlBody.ShouldContain("automatically cancelled");
    }

}
