using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.Entities;

[TestClass]
public sealed class EmailTemplateTests
{
    [TestMethod]
    public void Create_WithEventScope_SetsAllFields()
    {
        var scopeId = Guid.NewGuid();

        var template = EmailTemplate.Create(
            EmailSettingsScope.Event,
            scopeId,
            EmailTemplateType.Ticket,
            "Your ticket",
            "Text body",
            "<p>HTML body</p>");

        template.Id.Value.ShouldNotBe(Guid.Empty);
        template.Scope.ShouldBe(EmailSettingsScope.Event);
        template.ScopeId.ShouldBe(scopeId);
        template.Type.ShouldBe(EmailTemplateType.Ticket);
        template.Subject.ShouldBe("Your ticket");
        template.TextBody.ShouldBe("Text body");
        template.HtmlBody.ShouldBe("<p>HTML body</p>");
    }

    [TestMethod]
    public void Create_WithTeamScope_SetsScope()
    {
        var teamScopeId = Guid.NewGuid();

        var template = EmailTemplate.Create(
            EmailSettingsScope.Team,
            teamScopeId,
            EmailTemplateType.Ticket,
            "Subject",
            "Text",
            "<p>Html</p>");

        template.Scope.ShouldBe(EmailSettingsScope.Team);
        template.ScopeId.ShouldBe(teamScopeId);
    }

    [TestMethod]
    public void Create_TwoTemplates_HaveDistinctIds()
    {
        var t1 = EmailTemplate.Create(EmailSettingsScope.Event, Guid.NewGuid(), EmailTemplateType.Ticket, "S1", "T1", "H1");
        var t2 = EmailTemplate.Create(EmailSettingsScope.Event, Guid.NewGuid(), EmailTemplateType.Ticket, "S2", "T2", "H2");

        t1.Id.ShouldNotBe(t2.Id);
    }

    [TestMethod]
    public void Update_ChangesSubjectTextAndHtml()
    {
        var template = EmailTemplate.Create(
            EmailSettingsScope.Event,
            Guid.NewGuid(),
            EmailTemplateType.Ticket,
            "Old subject",
            "Old text",
            "<p>Old html</p>");

        template.Update("New subject", "New text", "<p>New html</p>");

        template.Subject.ShouldBe("New subject");
        template.TextBody.ShouldBe("New text");
        template.HtmlBody.ShouldBe("<p>New html</p>");
    }

    [TestMethod]
    public void Update_DoesNotChangeScope()
    {
        var scopeId = Guid.NewGuid();
        var template = EmailTemplate.Create(EmailSettingsScope.Event, scopeId, EmailTemplateType.Ticket, "S", "T", "H");

        template.Update("New subject", "New text", "<p>New html</p>");

        template.Scope.ShouldBe(EmailSettingsScope.Event);
        template.ScopeId.ShouldBe(scopeId);
    }
}
