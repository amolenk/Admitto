using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.ComposeTicketConfirmation;

[TestClass]
public sealed class TicketConfirmationEmailComposerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    // Given a complete event projection and no team branding projection
    // When a ticket confirmation is composed
    // Then the rendered confirmation is claimed and queued with derived public links and default branding
    [TestMethod]
    public async ValueTask ComposeAsync_CompleteEventContext_PreparesRenderedDelivery()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var fixture = TicketConfirmationEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var composer = fixture.BuildComposer(Environment);

        await composer.ComposeAsync(
            teamId,
            eventId,
            new TicketConfirmationIntent(registrationId, "Alice", ["General Admission"]),
            new TicketConfirmationDelivery("alice@example.com", "Alice Anderson", "ticket-confirmation:test"),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.Status.ShouldBe(EmailLogStatus.Pending);
        log.Subject.ShouldBe("Admitto: Your DevConf Ticket");

        var delivery = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        var textBody = delivery.Payload.RootElement.GetProperty("textBody").GetString()!;
        var htmlBody = delivery.Payload.RootElement.GetProperty("htmlBody").GetString()!;
        textBody.ShouldContain("Admitto");
        textBody.ShouldContain("DevConf");
        textBody.ShouldContain("General Admission");
        textBody.ShouldContain("https://public.example/e/devconf/qr-code/" + registrationId.Value);
        textBody.ShouldContain("https://public.example/e/devconf/edit/" + registrationId.Value);
        textBody.ShouldContain("https://public.example/e/devconf");
        htmlBody.ShouldContain("Admitto");
        htmlBody.ShouldContain("DevConf");
        htmlBody.ShouldContain("General Admission");
        htmlBody.ShouldContain("https://public.example/e/devconf/qr-code/" + registrationId.Value);
        htmlBody.ShouldContain("https://public.example/e/devconf/edit/" + registrationId.Value);
        htmlBody.ShouldContain("https://public.example/e/devconf");
        htmlBody.ShouldContain("#2563eb");
    }

    // Given an existing team context projection
    // When a ticket confirmation is composed
    // Then the projected team name appears in the subject and rendered bodies
    [TestMethod]
    public async ValueTask ComposeAsync_ExistingTeamContext_UsesProjectedTeamName()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TicketConfirmationEmailComposerFixture.ExistingTeamContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await fixture.BuildComposer(Environment).ComposeAsync(
            teamId,
            eventId,
            new TicketConfirmationIntent(RegistrationId.New(), "Alice", []),
            new TicketConfirmationDelivery("alice@example.com", "Alice Anderson", "ticket-confirmation:team-label"),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.Subject.ShouldBe("DevConf Team: Your DevConf Ticket");

        var delivery = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        delivery.Payload.RootElement.GetProperty("textBody").GetString()!
            .ShouldContain("DevConf Team");
        delivery.Payload.RootElement.GetProperty("htmlBody").GetString()!
            .ShouldContain("DevConf Team");
    }

    // Given an existing event projection missing a required rendering field
    // When a ticket confirmation is composed
    // Then it fails before creating an email claim or delivery message
    [TestMethod]
    public async ValueTask ComposeAsync_IncompleteEventContext_LeavesNoDeliveryClaim()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TicketConfirmationEmailComposerFixture.IncompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);
        var composer = fixture.BuildComposer(Environment);

        await Should.ThrowAsync<EventEmailContextMissingException>(async () =>
            await composer.ComposeAsync(
                teamId,
                eventId,
                new TicketConfirmationIntent(RegistrationId.New(), "Alice", []),
                new TicketConfirmationDelivery("alice@example.com", "Alice Anderson", "ticket-confirmation:missing"),
                testContext.CancellationToken));

        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
    }

    // Given a terminal ticket-confirmation claim and no event projection
    // When the same confirmation is redelivered
    // Then it short-circuits without loading context or creating delivery work
    [TestMethod]
    public async ValueTask ComposeAsync_TerminalClaimAndMissingContext_DoesNothing()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var delivery = new TicketConfirmationDelivery(
            "alice@example.com",
            "Alice Anderson",
            "ticket-confirmation:terminal");
        var fixture = TicketConfirmationEmailComposerFixture.IncompleteEventContext();
        await fixture.SeedTerminalClaimAsync(Environment, teamId, eventId, delivery);
        var composer = fixture.BuildComposer(Environment);

        await composer.ComposeAsync(
            teamId,
            eventId,
            new TicketConfirmationIntent(RegistrationId.New(), "Alice", []),
            delivery,
            testContext.CancellationToken);

        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken))
            .ShouldBe(1);
        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
    }

    // Given no event projection on the first delivery attempt
    // When the projection arrives and the confirmation is retried
    // Then the confirmation is rendered and queued successfully
    [TestMethod]
    public async ValueTask ComposeAsync_ContextArrivesAfterFailure_SucceedsOnRetry()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var fixture = TicketConfirmationEmailComposerFixture.CompleteEventContext();
        var composer = fixture.BuildComposer(Environment);
        var intent = new TicketConfirmationIntent(registrationId, "Alice", ["General Admission"]);
        var delivery = new TicketConfirmationDelivery(
            "alice@example.com",
            "Alice Anderson",
            "ticket-confirmation:retry");

        await Should.ThrowAsync<EventEmailContextMissingException>(async () =>
            await composer.ComposeAsync(teamId, eventId, intent, delivery, testContext.CancellationToken));

        await fixture.SetupAsync(Environment, teamId, eventId);
        await composer.ComposeAsync(teamId, eventId, intent, delivery, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken))
            .ShouldBe(1);
        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken))
            .ShouldBe(1);
    }

}
