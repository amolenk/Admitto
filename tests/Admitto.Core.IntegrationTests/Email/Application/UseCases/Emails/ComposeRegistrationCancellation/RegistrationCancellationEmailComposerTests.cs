using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeRegistrationCancellation;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.ComposeRegistrationCancellation;

[TestClass]
public sealed class RegistrationCancellationEmailComposerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    // Given a complete event context and an attendee-request cancellation
    // When the cancellation email is composed
    // Then the cancellation claim and rendered text and HTML are queued
    [TestMethod]
    public async ValueTask ComposeAsync_AttendeeRequest_PersistsRenderedCancellationDelivery()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var fixture = RegistrationCancellationEmailComposerFixture.ExistingTeamContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await fixture.BuildComposer(Environment).ComposeAsync(
            teamId,
            eventId,
            new AttendeeRequestCancellationIntent("Alice"),
            new RegistrationCancellationDelivery(
                "alice@example.com",
                "Alice Test",
                "registration-cancelled:attendee-request",
                registrationId),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().SingleAsync(
            testContext.CancellationToken);
        log.EmailType.ShouldBe(BuiltInEmailTemplateNames.Cancellation);
        log.Subject.ShouldBe("Your DevConf Registration Has Been Cancelled");

        var payload = await DeliveryPayloadAsync();
        payload.Text.ShouldContain("Hi Alice");
        payload.Text.ShouldContain("https://public.example/e/devconf/register");
        payload.Text.ShouldContain("https://devconf.example.com");
        payload.Html.ShouldContain("Hi Alice");
        payload.Html.ShouldContain("https://public.example/e/devconf/register");
        payload.Html.ShouldContain("href=\"https://devconf.example.com\"");
        payload.Html.ShouldNotContain("https://https://");
        payload.Html.ShouldContain("#0f766e");
    }

    // Given a complete event context and an automatic reconfirm cancellation
    // When the cancellation email is composed
    // Then the reconfirm-cancelled claim and rendered text and HTML are queued
    [TestMethod]
    public async ValueTask ComposeAsync_ReconfirmAutoCancel_PersistsRenderedReconfirmCancellationDelivery()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = RegistrationCancellationEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await fixture.BuildComposer(Environment).ComposeAsync(
            teamId,
            eventId,
            new ReconfirmAutoCancellationIntent("Alice"),
            Delivery(RegistrationId.New(), "registration-cancelled:reconfirm"),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().SingleAsync(
            testContext.CancellationToken);
        log.EmailType.ShouldBe(BuiltInEmailTemplateNames.ReconfirmCancelled);
        log.Subject.ShouldBe("Your DevConf Registration Has Been Cancelled");

        var payload = await DeliveryPayloadAsync();
        payload.Text.ShouldContain("automatically cancelled");
        payload.Text.ShouldContain("https://public.example/e/devconf/register");
        payload.Text.ShouldContain("https://devconf.example.com");
        payload.Html.ShouldContain("automatically cancelled");
        payload.Html.ShouldContain("https://public.example/e/devconf/register");
        payload.Html.ShouldContain("https://devconf.example.com");
    }

    // Given a complete event context and a denied visa letter
    // When the cancellation email is composed
    // Then the visa-letter-denied claim and rendered text and HTML are queued
    [TestMethod]
    public async ValueTask ComposeAsync_VisaLetterDenied_PersistsRenderedVisaDenialDelivery()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = RegistrationCancellationEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await fixture.BuildComposer(Environment).ComposeAsync(
            teamId,
            eventId,
            new VisaLetterDeniedCancellationIntent("Alice"),
            Delivery(RegistrationId.New(), "registration-cancelled:visa"),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().SingleAsync(
            testContext.CancellationToken);
        log.EmailType.ShouldBe(BuiltInEmailTemplateNames.VisaLetterDenied);
        log.Subject.ShouldBe("Your DevConf Registration Has Been Cancelled");

        var payload = await DeliveryPayloadAsync();
        payload.Text.ShouldContain("unable to provide business invitation letters");
        payload.Text.ShouldContain("Hi Alice");
        payload.Html.ShouldContain("unable to provide business invitation letters");
        payload.Html.ShouldContain("Hi Alice");
    }

    // Given an incomplete event context
    // When a cancellation email is composed
    // Then it fails before creating a claim or delivery message
    [TestMethod]
    public async ValueTask ComposeAsync_MissingEventContext_LeavesNoClaimOrDelivery()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = RegistrationCancellationEmailComposerFixture.IncompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await Should.ThrowAsync<EventEmailContextMissingException>(async () =>
            await fixture.BuildComposer(Environment).ComposeAsync(
                teamId,
                eventId,
                new AttendeeRequestCancellationIntent("Alice"),
                Delivery(RegistrationId.New(), "registration-cancelled:missing"),
                testContext.CancellationToken));

        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
    }

    // Given a terminal cancellation claim and no event context
    // When the cancellation is redelivered
    // Then it short-circuits without loading context or creating delivery work
    [TestMethod]
    public async ValueTask ComposeAsync_TerminalClaimAndMissingContext_DoesNothing()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var delivery = Delivery(RegistrationId.New(), "registration-cancelled:terminal");
        var fixture = RegistrationCancellationEmailComposerFixture.IncompleteEventContext();
        await fixture.SeedTerminalClaimAsync(
            Environment,
            teamId,
            eventId,
            delivery,
            BuiltInEmailTemplateNames.Cancellation);

        await fixture.BuildComposer(Environment).ComposeAsync(
            teamId,
            eventId,
            new AttendeeRequestCancellationIntent("Alice"),
            delivery,
            testContext.CancellationToken);

        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken))
            .ShouldBe(1);
        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
    }

    private static RegistrationCancellationDelivery Delivery(
        RegistrationId registrationId,
        string idempotencyKey) =>
        new("alice@example.com", "Alice Test", idempotencyKey, registrationId);

    private async ValueTask<(string Text, string Html)> DeliveryPayloadAsync()
    {
        var delivery = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        return (
            delivery.Payload.RootElement.GetProperty("textBody").GetString()!,
            delivery.Payload.RootElement.GetProperty("htmlBody").GetString()!);
    }
}
