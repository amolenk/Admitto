using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.Templating;

[TestClass]
public sealed class EmailTemplateServiceTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask LoadAsync_EventScopedTemplate_ReturnsEventTemplate()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var template = new EmailTemplateBuilder()
            .ForEvent(eventId)
            .WithSubject("Event subject")
            .Build();
        await Environment.Database.SeedAsync(db => db.EmailTemplates.Add(template));

        var service = new EmailTemplateService(Environment.Database.Context);
        var result = await service.LoadAsync(EmailTemplateType.Ticket, teamId, eventId, testContext.CancellationToken);

        result.Subject.ShouldBe("Event subject");
        result.Scope.ShouldBe(EmailSettingsScope.Event);
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
        await Environment.Database.SeedAsync(db => db.EmailTemplates.Add(template));

        var service = new EmailTemplateService(Environment.Database.Context);
        var result = await service.LoadAsync(EmailTemplateType.Ticket, teamId, eventId, testContext.CancellationToken);

        result.Subject.ShouldBe("Team subject");
        result.Scope.ShouldBe(EmailSettingsScope.Team);
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
            .ForEvent(eventId)
            .WithSubject("Event subject")
            .Build();
        await Environment.Database.SeedAsync(db =>
        {
            db.EmailTemplates.Add(teamTemplate);
            db.EmailTemplates.Add(eventTemplate);
        });

        var service = new EmailTemplateService(Environment.Database.Context);
        var result = await service.LoadAsync(EmailTemplateType.Ticket, teamId, eventId, testContext.CancellationToken);

        result.Subject.ShouldBe("Event subject");
        result.Scope.ShouldBe(EmailSettingsScope.Event);
    }

    [TestMethod]
    public async ValueTask LoadAsync_NoCustomTemplate_ReturnsBuiltInDefault()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var service = new EmailTemplateService(Environment.Database.Context);
        var result = await service.LoadAsync(EmailTemplateType.Ticket, teamId, eventId, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.TextBody.ShouldNotBeNullOrEmpty();
        result.HtmlBody.ShouldNotBeNullOrEmpty();
    }
}
