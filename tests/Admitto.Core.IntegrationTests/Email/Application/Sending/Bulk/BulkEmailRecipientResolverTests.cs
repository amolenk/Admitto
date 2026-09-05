using System.Text.Json;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Sending.Bulk;

[TestClass]
public sealed class BulkEmailRecipientResolverTests
{
    // Given an attendee filter and matching registration rows returned by the registrations facade
    // When recipients are resolved for a team and event
    // Then the filter is translated to the registrations query contract and each row is projected into a recipient
    [TestMethod]
    public async Task ResolveAsync_AttendeeFilter_MapsFilterToContractAndProjectsRows()
    {
        // Arrange
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        var ticketTypeId = Guid.NewGuid();
        var attendeeFilter = new BulkEmailAttendeeFilter(
            TicketTypeIds: [ticketTypeId],
            RegistrationStatus: RegistrationStatus.Registered,
            HasReconfirmed: false);

        var rows = new[]
        {
            new RegistrationListItemDto(
                RegistrationId: Guid.NewGuid(),
                Email: "Alice@Example.com",
                FirstName: "Alice",
                LastName: "Smith",
                TicketTypeIds: [Guid.NewGuid()],
                AdditionalDetails: new Dictionary<string, string> { ["company"] = "Acme" },
                CreatedAt: DateTimeOffset.UtcNow,
                RegistrationCycleId: Guid.NewGuid(),
                RegistrationVersion: 1,
                TicketCatalogVersion: 1,
                Status: RegistrationStatus.Registered,
                HasReconfirmed: false,
                ReconfirmedAt: null,
                EffectiveMaxReconfirmationEmails: null),
            new RegistrationListItemDto(
                RegistrationId: Guid.NewGuid(),
                Email: "bob@example.com",
                FirstName: "Bob",
                LastName: "Jones",
                TicketTypeIds: [],
                AdditionalDetails: new Dictionary<string, string>(),
                CreatedAt: DateTimeOffset.UtcNow,
                RegistrationCycleId: Guid.NewGuid(),
                RegistrationVersion: 1,
                TicketCatalogVersion: 1,
                Status: RegistrationStatus.Registered,
                HasReconfirmed: false,
                ReconfirmedAt: null,
                EffectiveMaxReconfirmationEmails: null),
        };

        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                teamId.Value,
                eventId.Value,
                Arg.Any<QueryRegistrationsDto>(),
                Arg.Any<CancellationToken>())
            .Returns(rows);

        var resolver = new BulkEmailRecipientResolver(facade);

        // Act
        var recipients = await resolver.ResolveAsync(
            teamId, eventId, attendeeFilter, CancellationToken.None);

        // Assert — the Email-owned filter is translated to the Registrations query contract.
        await facade.Received(1).GetRegistrationsAsync(
            teamId.Value,
            eventId.Value,
            Arg.Is<QueryRegistrationsDto>(q =>
                q != null
                && q.TicketTypeIds!.Contains(ticketTypeId)
                && q.RegistrationStatus == RegistrationStatus.Registered
                && q.HasReconfirmed == false),
            Arg.Any<CancellationToken>());

        recipients.Count.ShouldBe(2);

        var alice = recipients[0];
        alice.Email.ShouldBe(EmailAddress.From("Alice@Example.com"));
        alice.DisplayName.ShouldBe("Alice Smith");
        alice.RegistrationId.ShouldBe(RegistrationId.From(rows[0].RegistrationId));
        var aliceParams = JsonSerializer.Deserialize<JsonElement>(alice.ParametersJson);
        aliceParams.GetProperty("first_name").GetString().ShouldBe("Alice");
        aliceParams.GetProperty("last_name").GetString().ShouldBe("Smith");
        aliceParams.GetProperty("email").GetString().ShouldBe("Alice@Example.com");

        var bob = recipients[1];
        bob.DisplayName.ShouldBe("Bob Jones");
        bob.RegistrationId.ShouldBe(RegistrationId.From(rows[1].RegistrationId));
    }

    // Given two registrations with only one matching the expected cycle snapshot
    // When reconfirm recipients are resolved
    // Then the stale-cycle registration is excluded
    [TestMethod]
    public async Task ResolveAsync_ReconfirmCycleMismatch_ExcludesStaleRecipient()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        var firstRegistrationId = Guid.NewGuid();
        var secondRegistrationId = Guid.NewGuid();
        var expectedCycleId = Guid.NewGuid();
        var rows = new[]
        {
            new RegistrationListItemDto(
                firstRegistrationId, "alice@example.com", "Alice", "Smith", [],
                new Dictionary<string, string>(), DateTimeOffset.UtcNow,
                expectedCycleId, 1, 1, RegistrationStatus.Registered, false, null, 1),
            new RegistrationListItemDto(
                secondRegistrationId, "bob@example.com", "Bob", "Jones", [],
                new Dictionary<string, string>(), DateTimeOffset.UtcNow,
                Guid.NewGuid(), 1, 1, RegistrationStatus.Registered, false, null, 1)
        };
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                teamId.Value, eventId.Value, Arg.Any<QueryRegistrationsDto>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var resolver = new BulkEmailRecipientResolver(facade);
        var recipients = await resolver.ResolveAsync(
            teamId,
            eventId,
            new BulkEmailAttendeeFilter(
                RegistrationStatus: RegistrationStatus.Registered,
                HasReconfirmed: false,
                RegistrationIds: [firstRegistrationId, secondRegistrationId],
                RegistrationCycleIds: new Dictionary<Guid, Guid>
                {
                    [firstRegistrationId] = expectedCycleId,
                    [secondRegistrationId] = expectedCycleId
                }),
            CancellationToken.None);

        recipients.ShouldHaveSingleItem().RegistrationId.ShouldBe(RegistrationId.From(firstRegistrationId));
    }

    // Given an attendee filter for which the registrations facade returns no rows
    // When recipients are resolved for a team and event
    // Then an empty list is returned
    [TestMethod]
    public async Task ResolveAsync_AttendeeFilter_NoMatches_ReturnsEmptyList()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(TeamId.New().Value, TicketedEventId.New().Value, default!, default)
            .ReturnsForAnyArgs(Array.Empty<RegistrationListItemDto>());

        var resolver = new BulkEmailRecipientResolver(facade);

        var recipients = await resolver.ResolveAsync(
            TeamId.New(),
            TicketedEventId.New(),
            new BulkEmailAttendeeFilter(),
            CancellationToken.None);

        recipients.ShouldBeEmpty();
    }
}
