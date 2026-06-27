using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Registrations.Application.PublicEventLinks;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;

[TestClass]
public sealed class GetTicketedEventEmailContextTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask GetTicketedEventEmailContext_TwoSelfServiceTicketTypes_IncludesChangeTicketsLink()
    {
        var fixture = await SeedEventAsync(selfServiceTicketCount: 2);
        var sut = CreateHandler("https://tickets.admitto.org", "#0f766e");
        var registrationId = Guid.NewGuid();

        var result = await sut.HandleAsync(
            new GetTicketedEventEmailContextQuery(fixture.TeamId.Value, fixture.EventId.Value, registrationId),
            testContext.CancellationToken);

        result.PublicEventLink.ShouldBe("https://tickets.admitto.org/e/azure-fest-2026");
        result.QRCodeLink.ShouldStartWith("https://tickets.admitto.org/e/azure-fest-2026/");
        result.CancelLink.ShouldStartWith("https://tickets.admitto.org/e/azure-fest-2026/");
        result.ChangeTicketsLink.ShouldBe($"https://tickets.admitto.org/e/azure-fest-2026/registrations/{registrationId}/tickets");
        result.TeamAccentColor.ShouldBe("#0f766e");
    }

    [TestMethod]
    public async ValueTask GetTicketedEventEmailContext_OneSelfServiceTicketType_OmitsChangeTicketsLink()
    {
        var fixture = await SeedEventAsync(selfServiceTicketCount: 1);
        var sut = CreateHandler("https://tickets.admitto.org", "#0f766e");

        var result = await sut.HandleAsync(
            new GetTicketedEventEmailContextQuery(fixture.TeamId.Value, fixture.EventId.Value, Guid.NewGuid()),
            testContext.CancellationToken);

        result.ChangeTicketsLink.ShouldBeNull();
    }

    private async ValueTask<(TeamId TeamId, TicketedEventId EventId)> SeedEventAsync(int selfServiceTicketCount)
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            teamId,
            EventName.From("Azure Fest 2026"),
            AbsoluteUrl.From("https://azurefest.example.com"),
            AbsoluteUrl.From("https://event.example.com"),
            Slug.From("azure-fest-2026"),
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            TimeZoneId.From("UTC"));
        var catalog = TicketCatalog.Create(eventId, teamId);
        for (var i = 0; i < selfServiceTicketCount; i++)
        {
            catalog.AddTicketType(TicketTypeId.New(), TicketTypeName.From($"Public {i}"), [], 100, selfServiceEnabled: true);
        }

        catalog.AddTicketType(TicketTypeId.New(), TicketTypeName.From("Private"), [], 100, selfServiceEnabled: false);

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
        });

        return (teamId, eventId);
    }

    private GetTicketedEventEmailContextHandler CreateHandler(string publicBaseUrl, string accentColor)
    {
        var organizationFacade = Substitute.For<IOrganizationFacade>();
        organizationFacade
            .GetTeamBrandingAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => new TeamBrandingDto(call.ArgAt<Guid>(0), accentColor));

        return new GetTicketedEventEmailContextHandler(
            Environment.RegistrationsDatabase.Context,
            organizationFacade,
            Options.Create(new PublicTicketsOptions { BaseUrl = publicBaseUrl }));
    }
}
