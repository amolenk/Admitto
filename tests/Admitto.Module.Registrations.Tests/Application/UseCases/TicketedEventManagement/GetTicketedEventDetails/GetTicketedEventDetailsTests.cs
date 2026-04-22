using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails;

[TestClass]
public sealed class GetTicketedEventDetailsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_GetTicketedEventDetails_IncludesAllPolicies()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        var opensAt = DateTimeOffset.UtcNow.AddDays(1);
        var closesAt = DateTimeOffset.UtcNow.AddDays(10);
        var cancelCutoff = DateTimeOffset.UtcNow.AddDays(20);
        var reconfirmOpens = DateTimeOffset.UtcNow.AddDays(11);
        var reconfirmCloses = DateTimeOffset.UtcNow.AddDays(25);

        await Environment.Database.SeedAsync(ctx =>
        {
            var te = TicketedEvent.Create(
                eventId,
                teamId,
                Slug.From("conf-2026"),
                DisplayName.From("Conf 2026"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31));

            te.ConfigureRegistrationPolicy(
                TicketedEventRegistrationPolicy.Create(opensAt, closesAt, "@example.com"));
            te.ConfigureCancellationPolicy(new TicketedEventCancellationPolicy(cancelCutoff));
            te.ConfigureReconfirmPolicy(
                TicketedEventReconfirmPolicy.Create(reconfirmOpens, reconfirmCloses, TimeSpan.FromDays(7)));

            ctx.TicketedEvents.Add(te);
        });

        var sut = new GetTicketedEventDetailsHandler(Environment.Database.Context, TimeProvider.System);

        var result = await sut.HandleAsync(
            new GetTicketedEventDetailsQuery(eventId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(eventId.Value);
        result.TeamId.ShouldBe(teamId.Value);
        result.Slug.ShouldBe("conf-2026");
        result.Status.ShouldBe(EventLifecycleStatus.Active);

        result.RegistrationPolicy.ShouldNotBeNull();
        result.RegistrationPolicy.OpensAt.ShouldBe(opensAt);
        result.RegistrationPolicy.ClosesAt.ShouldBe(closesAt);
        result.RegistrationPolicy.AllowedEmailDomain.ShouldBe("@example.com");

        result.CancellationPolicy.ShouldNotBeNull();
        result.CancellationPolicy.LateCancellationCutoff.ShouldBe(cancelCutoff);

        result.ReconfirmPolicy.ShouldNotBeNull();
        result.ReconfirmPolicy.OpensAt.ShouldBe(reconfirmOpens);
        result.ReconfirmPolicy.ClosesAt.ShouldBe(reconfirmCloses);
        result.ReconfirmPolicy.CadenceDays.ShouldBe(7);
    }

    [TestMethod]
    public async ValueTask SC002_GetTicketedEventDetails_WithoutPolicies_ReturnsNullPolicies()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        await Environment.Database.SeedAsync(ctx =>
        {
            var te = TicketedEvent.Create(
                eventId,
                teamId,
                Slug.From("bare-event"),
                DisplayName.From("Bare Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31));
            ctx.TicketedEvents.Add(te);
        });

        var sut = new GetTicketedEventDetailsHandler(Environment.Database.Context, TimeProvider.System);

        var result = await sut.HandleAsync(
            new GetTicketedEventDetailsQuery(eventId),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.RegistrationPolicy.ShouldBeNull();
        result.CancellationPolicy.ShouldBeNull();
        result.ReconfirmPolicy.ShouldBeNull();
        result.IsRegistrationOpen.ShouldBeFalse();
    }

    [TestMethod]
    public async ValueTask SC003_GetTicketedEventDetails_NotFound_ReturnsNull()
    {
        var sut = new GetTicketedEventDetailsHandler(Environment.Database.Context, TimeProvider.System);

        var result = await sut.HandleAsync(
            new GetTicketedEventDetailsQuery(TicketedEventId.New()),
            testContext.CancellationToken);

        result.ShouldBeNull();
    }
}
