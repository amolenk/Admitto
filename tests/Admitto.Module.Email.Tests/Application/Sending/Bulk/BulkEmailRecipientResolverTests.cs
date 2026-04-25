using System.Text.Json;
using Amolenk.Admitto.Module.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.Sending.Bulk;

[TestClass]
public sealed class BulkEmailRecipientResolverTests
{
    [TestMethod]
    public async Task ResolveAsync_AttendeeSource_PassesFilterToFacadeAndProjectsRows()
    {
        // Arrange
        var eventId = TicketedEventId.New();
        var filter = new QueryRegistrationsDto(
            TicketTypeSlugs: ["regular"],
            RegistrationStatus: RegistrationStatus.Registered,
            HasReconfirmed: false);

        var rows = new[]
        {
            new RegistrationListItemDto(
                RegistrationId: Guid.NewGuid(),
                Email: "Alice@Example.com",
                FirstName: "Alice",
                LastName: "Smith",
                TicketTypeSlugs: ["regular"],
                AdditionalDetails: new Dictionary<string, string> { ["company"] = "Acme" },
                Status: RegistrationStatus.Registered,
                HasReconfirmed: false,
                ReconfirmedAt: null),
            new RegistrationListItemDto(
                RegistrationId: Guid.NewGuid(),
                Email: "bob@example.com",
                FirstName: "Bob",
                LastName: "",
                TicketTypeSlugs: [],
                AdditionalDetails: new Dictionary<string, string>(),
                Status: RegistrationStatus.Registered,
                HasReconfirmed: false,
                ReconfirmedAt: null),
        };

        var facade = Substitute.For<IRegistrationsFacade>();
        facade.QueryRegistrationsAsync(eventId, filter, Arg.Any<CancellationToken>())
            .Returns(rows);

        var resolver = new BulkEmailRecipientResolver(facade);

        // Act
        var recipients = await resolver.ResolveAsync(
            eventId, new AttendeeSource(filter), CancellationToken.None);

        // Assert
        await facade.Received(1)
            .QueryRegistrationsAsync(eventId, filter, Arg.Any<CancellationToken>());
        recipients.Count.ShouldBe(2);

        var alice = recipients[0];
        alice.Email.ShouldBe("Alice@Example.com");
        alice.DisplayName.ShouldBe("Alice Smith");
        alice.RegistrationId.ShouldBe(rows[0].RegistrationId);
        var aliceParams = JsonSerializer.Deserialize<JsonElement>(alice.ParametersJson);
        aliceParams.GetProperty("first_name").GetString().ShouldBe("Alice");
        aliceParams.GetProperty("last_name").GetString().ShouldBe("Smith");
        aliceParams.GetProperty("email").GetString().ShouldBe("Alice@Example.com");

        var bob = recipients[1];
        bob.DisplayName.ShouldBe("Bob");
    }

    [TestMethod]
    public async Task ResolveAsync_ExternalListSource_ReturnsLiteralItems()
    {
        var resolver = new BulkEmailRecipientResolver(Substitute.For<IRegistrationsFacade>());
        var source = new ExternalListSource(
        [
            new ExternalListItem("alice@example.com", "Alice"),
            new ExternalListItem("bob@example.com", DisplayName: null),
        ]);

        var recipients = await resolver.ResolveAsync(
            TicketedEventId.New(), source, CancellationToken.None);

        recipients.Count.ShouldBe(2);
        recipients[0].Email.ShouldBe("alice@example.com");
        recipients[0].DisplayName.ShouldBe("Alice");
        recipients[0].RegistrationId.ShouldBeNull();
        recipients[1].DisplayName.ShouldBeNull();

        var bobParams = JsonSerializer.Deserialize<JsonElement>(recipients[1].ParametersJson);
        bobParams.GetProperty("email").GetString().ShouldBe("bob@example.com");
        bobParams.GetProperty("display_name").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [TestMethod]
    public async Task ResolveAsync_AttendeeSource_NoMatches_ReturnsEmptyList()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.QueryRegistrationsAsync(
                Arg.Any<TicketedEventId>(),
                Arg.Any<QueryRegistrationsDto>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RegistrationListItemDto>());

        var resolver = new BulkEmailRecipientResolver(facade);

        var recipients = await resolver.ResolveAsync(
            TicketedEventId.New(),
            new AttendeeSource(new QueryRegistrationsDto()),
            CancellationToken.None);

        recipients.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ResolveAsync_ExternalListSource_Empty_ReturnsEmptyList()
    {
        var resolver = new BulkEmailRecipientResolver(Substitute.For<IRegistrationsFacade>());

        var recipients = await resolver.ResolveAsync(
            TicketedEventId.New(),
            new ExternalListSource(Array.Empty<ExternalListItem>()),
            CancellationToken.None);

        recipients.ShouldBeEmpty();
    }
}
