using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.TicketTypes;

[TestClass]
public sealed class TicketTypeManagementTests(TestContext testContext) : EndToEndTestBase
{
    // Given a ticketed event
    // When a new ticket type is added with a valid max reconfirm attempts value
    // Then the API returns 201 Created and the value is persisted on the ticket type
    [TestMethod]
    public async Task AddTicketType_WithValidMaxReconfirmAttempts_PersistsValue()
    {
        var fixture = TicketTypeManagementFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.TicketTypesRoute,
            new
            {
                Name = "Session",
                TimeSlots = Array.Empty<string>(),
                MaxCapacity = 50,
                MaxReconfirmAttempts = 3
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>(testContext.CancellationToken);
        body.ShouldNotBeNull();

        await Environment.RegistrationsDatabase.WithContextAsync(async db =>
        {
            var catalog = await db.TicketCatalogs.AsNoTracking()
                .SingleAsync(c => c.Id == TicketedEventId.From(fixture.EventId), testContext.CancellationToken);
            var ticketType = catalog.TicketTypes.Single(t => t.Id == TicketTypeId.From(body.Id));
            ticketType.MaxReconfirmAttempts.ShouldBe(3);
        });
    }

    // Given a ticketed event
    // When a new ticket type is added with a max reconfirm attempts value of zero
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task AddTicketType_WithZeroMaxReconfirmAttempts_Returns400()
    {
        var fixture = TicketTypeManagementFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.TicketTypesRoute,
            new
            {
                Name = "Session",
                TimeSlots = Array.Empty<string>(),
                MaxCapacity = 50,
                MaxReconfirmAttempts = 0
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given a ticketed event
    // When a new ticket type is added without specifying a max reconfirm attempts value
    // Then the API returns 201 Created and the ticket type's max reconfirm attempts is persisted as null
    [TestMethod]
    public async Task AddTicketType_WithoutMaxReconfirmAttempts_PersistsNull()
    {
        var fixture = TicketTypeManagementFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.TicketTypesRoute,
            new
            {
                Name = "Session",
                TimeSlots = Array.Empty<string>(),
                MaxCapacity = 50
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>(testContext.CancellationToken);
        body.ShouldNotBeNull();

        await Environment.RegistrationsDatabase.WithContextAsync(async db =>
        {
            var catalog = await db.TicketCatalogs.AsNoTracking()
                .SingleAsync(c => c.Id == TicketedEventId.From(fixture.EventId), testContext.CancellationToken);
            var ticketType = catalog.TicketTypes.Single(t => t.Id == TicketTypeId.From(body.Id));
            ticketType.MaxReconfirmAttempts.ShouldBeNull();
        });
    }

    // Given an existing ticket type on a ticketed event
    // When the ticket type is updated with a valid max reconfirm attempts value
    // Then the API returns 204 No Content and the value is persisted on the ticket type
    [TestMethod]
    public async Task UpdateTicketType_WithValidMaxReconfirmAttempts_PersistsValue()
    {
        var fixture = TicketTypeManagementFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.ExistingTicketTypeRoute,
            new
            {
                MaxReconfirmAttempts = 4
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await Environment.RegistrationsDatabase.WithContextAsync(async db =>
        {
            var catalog = await db.TicketCatalogs.AsNoTracking()
                .SingleAsync(c => c.Id == TicketedEventId.From(fixture.EventId), testContext.CancellationToken);
            var ticketType = catalog.TicketTypes.Single(t => t.Id == TicketTypeManagementFixture.ExistingTicketTypeId);
            ticketType.MaxReconfirmAttempts.ShouldBe(4);
        });
    }

    // Given an existing ticket type on a ticketed event
    // When the ticket type is updated with a max reconfirm attempts value of zero
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task UpdateTicketType_WithZeroMaxReconfirmAttempts_Returns400()
    {
        var fixture = TicketTypeManagementFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.ExistingTicketTypeRoute,
            new
            {
                MaxReconfirmAttempts = 0
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given a ticket type that is sold out
    // When the ticket type is updated to enable the waitlist
    // Then the API returns 204 No Content and the ticket type has an active waitlist
    [TestMethod]
    public async Task UpdateTicketType_EnableWaitlistOnSoldOutTicketType_PersistsWaitlist()
    {
        var fixture = TicketTypeManagementFixture.WithSoldOutTicketType();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.ExistingTicketTypeRoute,
            new
            {
                MaxCapacity = 1,
                WaitlistEnabled = true
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await Environment.RegistrationsDatabase.WithContextAsync(async db =>
        {
            var catalog = await db.TicketCatalogs.AsNoTracking()
                .SingleAsync(c => c.Id == TicketedEventId.From(fixture.EventId), testContext.CancellationToken);
            var ticketType = catalog.TicketTypes.Single(t => t.Id == TicketTypeManagementFixture.ExistingTicketTypeId);
            ticketType.WaitlistEnabled.ShouldBeTrue();
            ticketType.WaitlistMode.ShouldBeTrue();

            var waitlist = await db.Waitlists.AsNoTracking()
                .SingleAsync(w => w.Id == TicketTypeManagementFixture.ExistingTicketTypeId,
                    testContext.CancellationToken);
            waitlist.EventId.ShouldBe(TicketedEventId.From(fixture.EventId));
        });
    }

    private sealed record IdResponse(Guid Id);
}
