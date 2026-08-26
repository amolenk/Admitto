using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;

[TestClass]
public sealed class UpdatePartnerRegistrationHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    private UpdatePartnerRegistrationHandler CreateSut() =>
        new(Environment.RegistrationsDatabase.Context, TimeProvider.System);

    // Given a partner registration with early-bird and workshop ticket types having available capacity
    // When a valid update command changes the ticket selection and additional details
    // Then the registration's details and tickets are persisted and ticket-type capacities are adjusted
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_ValidInput_PersistsDetailsTicketsAndCapacity()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithCapacity(
            earlyBirdMax: 100,
            earlyBirdUsed: 50,
            workshopMax: 20,
            workshopUsed: 10);
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("workshop").Value],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.LastName.ShouldBe(LastName.From("Anderson"));
            registration.AdditionalDetails["dietary"].ShouldBe("vegan");
            registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.GetTicketTypeId("workshop"));

            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.GetTicketType(fixture.GetTicketTypeId("early-bird"))!.UsedCapacity.ShouldBe(49);
            catalog.GetTicketType(fixture.GetTicketTypeId("workshop"))!.UsedCapacity.ShouldBe(11);
        });
    }

    // Given a partner registration with capacity configured
    // When an update command keeps the same ticket selection and only changes additional details
    // Then the details are updated and no tickets-changed domain event is raised
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_DetailsOnly_DoesNotRaiseTicketChangeEvent()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithCapacity();
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        var registration = await Environment.RegistrationsDatabase.Context.Registrations
            .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);

        registration.LastName.ShouldBe(LastName.From("Anderson"));
        registration.GetDomainEvents().OfType<TicketsChangedDomainEvent>().ShouldBeEmpty();
    }

    // Given a partner registration with capacity configured
    // When an update command includes an additional-details key that is not in the schema
    // Then a key-not-in-schema error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_UnknownAdditionalDetailKey_ThrowsAndLeavesRegistrationUnchanged()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithCapacity();
        await fixture.SetupAsync(Environment);

        var command = ValidWorkshopCommand(fixture) with
        {
            AdditionalDetails = new Dictionary<string, string> { ["shoesize"] = "44" }
        };

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(AdditionalDetails.Errors.KeyNotInSchema("shoesize"));
        await AssertRegistrationStillOriginal(fixture);
    }

    // Given a partner registration with capacity configured
    // When an update command includes an additional-details value that exceeds the allowed length
    // Then a value-too-long error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_AdditionalDetailValueTooLong_ThrowsAndLeavesRegistrationUnchanged()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithCapacity();
        await fixture.SetupAsync(Environment);

        var command = ValidWorkshopCommand(fixture) with
        {
            AdditionalDetails = new Dictionary<string, string> { ["tshirt"] = "XXXXL-extra-long" }
        };

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(AdditionalDetails.Errors.ValueTooLong("tshirt", 5));
        await AssertRegistrationStillOriginal(fixture);
    }

    // Given a partner registration where the requested workshop ticket type is sold out
    // When an update command requests that sold-out ticket type
    // Then an at-capacity error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_CapacityFull_ThrowsAndLeavesRegistrationUnchanged()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithSoldOutWorkshop();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(ValidWorkshopCommand(fixture), testContext.CancellationToken));

        result.Error.ShouldMatch(TicketType.Errors.TicketTypeAtCapacity(fixture.GetTicketTypeId("workshop")));
        await AssertRegistrationStillOriginal(fixture);
    }

    // Given a partner registration with a waitlist coupon that offers a specific ticket type
    // When an update command omits the ticket type offered by the waitlist coupon
    // Then a waitlist-coupon-ticket-missing error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_WaitlistCouponOfferedTicketMissing_ThrowsAndLeavesRegistrationUnchanged()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithWaitlistCoupon();
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value],
            new Dictionary<string, string> { ["dietary"] = "vegan" },
            fixture.WaitlistCouponCode);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(UpdatePartnerRegistrationHandler.Errors.WaitlistCouponTicketMissing(fixture.GetTicketTypeId("workshop")));
        await AssertRegistrationStillOriginal(fixture);
    }

    // Given a partner registration where the workshop ticket type has self-service updates disabled
    // When an update command requests that ticket type
    // Then a ticket-types-not-self-service error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_SelfServiceDisabledTicket_ThrowsAndLeavesRegistrationUnchanged()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithSelfServiceDisabledWorkshop();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(ValidWorkshopCommand(fixture), testContext.CancellationToken));

        result.Error.ShouldMatch(TicketCatalog.Errors.TicketTypesNotSelfService([fixture.GetTicketTypeId("workshop").Value]));
        await AssertRegistrationStillOriginal(fixture);
    }

    // Given a registration that has already been cancelled
    // When an update command is handled for it
    // Then a registration-is-cancelled error is thrown
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_CancelledRegistration_ThrowsRegistrationIsCancelled()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(ValidEarlyBirdCommand(fixture), testContext.CancellationToken));

        result.Error.ShouldMatch(UpdatePartnerRegistrationHandler.Errors.RegistrationIsCancelled);
    }

    private static UpdatePartnerRegistrationCommand ValidWorkshopCommand(UpdatePartnerRegistrationFixture fixture) =>
        new(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("workshop").Value],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

    private static UpdatePartnerRegistrationCommand ValidEarlyBirdCommand(UpdatePartnerRegistrationFixture fixture) =>
        new(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

    private async ValueTask AssertRegistrationStillOriginal(UpdatePartnerRegistrationFixture fixture)
    {
        await Environment.RegistrationsDatabase.WithContextAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);

            registration.FirstName.ShouldBe(FirstName.From("Alice"));
            registration.LastName.ShouldBe(LastName.From("Test"));
            registration.AdditionalDetails["dietary"].ShouldBe("old");
            registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.GetTicketTypeId("early-bird"));
        });
    }
}
