using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Email.Domain.Tests.Entities;

[TestClass]
public sealed class EmailTemplateTests
{
    [TestMethod]
    public void Create_WithEventScope_SetsAllFields()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var template = EmailTemplate.Create(
            teamId,
            eventId,
            BuiltInEmailTemplateNames.TicketConfirmation,
            "Your ticket",
            "Text body",
            "<p>HTML body</p>");

        template.Id.Value.ShouldNotBe(Guid.Empty);
        template.TeamId.ShouldBe(teamId);
        template.TicketedEventId.ShouldBe(eventId);
        template.Name.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        template.Subject.ShouldBe("Your ticket");
        template.TextBody.ShouldBe("Text body");
        template.HtmlBody.ShouldBe("<p>HTML body</p>");
    }

    [TestMethod]
    public void Create_WithTeamScope_SetsScope()
    {
        var teamId = TeamId.New();

        var template = EmailTemplate.Create(
            teamId,
            null,
            BuiltInEmailTemplateNames.TicketConfirmation,
            "Subject",
            "Text",
            "<p>Html</p>");

        template.TeamId.ShouldBe(teamId);
        template.TicketedEventId.ShouldBeNull();
    }

    [TestMethod]
    public void Create_TwoTemplates_HaveDistinctIds()
    {
        var t1EventId = TicketedEventId.New();
        var t2EventId = TicketedEventId.New();
        var t1 = EmailTemplate.Create(TeamId.New(), t1EventId, BuiltInEmailTemplateNames.TicketConfirmation, "S1", "T1", "H1");
        var t2 = EmailTemplate.Create(TeamId.New(), t2EventId, BuiltInEmailTemplateNames.Reconfirmation, "S2", "T2", "H2");

        t1.Id.ShouldNotBe(t2.Id);
    }

    [TestMethod]
    public void Update_ChangesSubjectTextAndHtml()
    {
        var template = EmailTemplate.Create(
            TeamId.New(),
            TicketedEventId.New(),
            BuiltInEmailTemplateNames.TicketConfirmation,
            "Old subject",
            "Old text",
            "<p>Old html</p>");

        template.Update("New subject", "New text", "<p>New html</p>");

        template.Subject.ShouldBe("New subject");
        template.TextBody.ShouldBe("New text");
        template.HtmlBody.ShouldBe("<p>New html</p>");
    }

    [TestMethod]
    public void Update_DoesNotChangeScopeIds()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var template = EmailTemplate.Create(teamId, eventId, BuiltInEmailTemplateNames.TicketConfirmation, "S", "T", "H");

        template.Update("New subject", "New text", "<p>New html</p>");

        template.TeamId.ShouldBe(teamId);
        template.TicketedEventId.ShouldBe(eventId);
    }
}
