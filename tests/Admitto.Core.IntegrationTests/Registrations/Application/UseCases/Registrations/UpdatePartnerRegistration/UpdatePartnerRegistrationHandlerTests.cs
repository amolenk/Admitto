using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReleaseTickets;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService;
using Amolenk.Admitto.Core.Registrations.Contracts;
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
            [],
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
            catalog.GetTicketType(fixture.GetTicketTypeId("early-bird"))!.PublicUsedCapacity.ShouldBe(49);
            catalog.GetTicketType(fixture.GetTicketTypeId("workshop"))!.PublicUsedCapacity.ShouldBe(11);
        });
    }

    // Given a registration holding an admin ticket
    // When the attendee updates their tickets via the partner API, keeping the admin ticket
    // Then the kept ticket stays an admin ticket, so releasing it later frees no public seat
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_KeepsExistingAdminTicket_PreservesClaimModeOnRelease()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithAdminTicket();
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("vip").Value, fixture.GetTicketTypeId("early-bird").Value],
            [],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Tickets.Count.ShouldBe(2);
            var vipTicket = registration.Tickets.Single(t => t.Id == fixture.GetTicketTypeId("vip"));
            vipTicket.Mode.ShouldBe(ClaimMode.Admin);
            var earlyBirdTicket = registration.Tickets.Single(t => t.Id == fixture.GetTicketTypeId("early-bird"));
            earlyBirdTicket.Mode.ShouldBe(ClaimMode.Public);
        });

        var releaseHandler = new ReleaseTicketsHandler(Environment.RegistrationsDatabase.Context);
        await releaseHandler.HandleAsync(
            new ReleaseTicketsCommand(fixture.RegistrationId.Value, fixture.EventId.Value, fixture.TeamId.Value),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            var vip = catalog.GetTicketType(fixture.GetTicketTypeId("vip"))!;
            vip.PublicUsedCapacity.ShouldBe(0);
            vip.AdminUsedCount.ShouldBe(0);
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
            [],
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
    // Then a ticket-state-conflict error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_CapacityFull_ThrowsAndLeavesRegistrationUnchanged()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithSoldOutWorkshop();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(ValidWorkshopCommand(fixture), testContext.CancellationToken));

        result.Error.ShouldMatch(RegisterAttendeeSelfServiceHandler.Errors.TicketStateConflict(
            new RegisterAttendeeSelfServiceHandler.TicketStateConflict(
                RegisterableTicketTypeIds: [],
                WaitlistableTicketTypeIds: [],
                UnavailableTicketTypeIds: [fixture.GetTicketTypeId("workshop").Value],
                UnknownTicketTypeIds: [],
                InvalidForRequestedActionTicketTypeIds: [])));
        await AssertRegistrationStillOriginal(fixture);
    }

    // Given a waitlist coupon for a sold-out workshop issued to the attendee, with another attendee still queued
    // When the attendee updates their registration to add the workshop using the coupon
    // Then the workshop is claimed from the public pool, the coupon and its waitlist coupon are redeemed, and the other attendee stays queued
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_WaitlistCoupon_ClaimsOfferedTicketAndRedeemsCoupon()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithWaitlistCoupon();
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value, fixture.GetTicketTypeId("workshop").Value],
            [],
            new Dictionary<string, string> { ["dietary"] = "vegan" },
            fixture.CouponCode);

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.Tickets.ShouldContain(t =>
                t.Id == fixture.GetTicketTypeId("workshop") && t.Mode == ClaimMode.Public);

            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldNotBeNull();

            var catalog = await dbContext.TicketCatalogs
                .FirstAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            var workshop = catalog.GetTicketType(fixture.GetTicketTypeId("workshop"))!;
            workshop.PublicUsedCapacity.ShouldBe(2);
            workshop.AdminUsedCount.ShouldBe(0);
            workshop.WaitlistHeldCapacity.ShouldBe(0);

            var waitlist = await dbContext.Waitlists
                .FirstAsync(w => w.Id == fixture.GetTicketTypeId("workshop"), testContext.CancellationToken);
            waitlist.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Redeemed);
            waitlist.HasActiveEntry(UpdatePartnerRegistrationFixture.AttendeeEmail).ShouldBeFalse();
            waitlist.GetActivePosition(UpdatePartnerRegistrationFixture.OtherQueuedEmail).ShouldBe(1);
        });
    }

    // Given an organiser coupon covering two sold-out ticket types, both of whose waitlists the attendee is on
    // When the attendee updates their registration to confirm only one of them using the coupon, staying queued for the other
    // Then only that ticket type is granted as an admin ticket, its waitlist entry is removed, the other ticket type is forfeited, and the coupon is redeemed
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_OrganiserMultiTicketTypeCoupon_GrantsSelectedTicketAndRemovesItsWaitlistEntry()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithOrganiserMultiTicketTypeCoupon();
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value, fixture.GetTicketTypeId("workshop").Value],
            [fixture.GetTicketTypeId("masterclass").Value],
            new Dictionary<string, string> { ["dietary"] = "vegan" },
            fixture.CouponCode);

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.Tickets.Select(t => t.Id).ShouldBe(
                [fixture.GetTicketTypeId("early-bird"), fixture.GetTicketTypeId("workshop")],
                ignoreOrder: true);
            registration.Tickets.ShouldContain(t =>
                t.Id == fixture.GetTicketTypeId("workshop") && t.Mode == ClaimMode.Admin);

            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldNotBeNull();

            var catalog = await dbContext.TicketCatalogs
                .FirstAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            var workshop = catalog.GetTicketType(fixture.GetTicketTypeId("workshop"))!;
            workshop.PublicUsedCapacity.ShouldBe(1);
            workshop.AdminUsedCount.ShouldBe(1);
            catalog.GetTicketType(fixture.GetTicketTypeId("masterclass"))!.PublicUsedCapacity.ShouldBe(1);

            var workshopWaitlist = await dbContext.Waitlists
                .FirstAsync(w => w.Id == fixture.GetTicketTypeId("workshop"), testContext.CancellationToken);
            workshopWaitlist.HasActiveEntry(UpdatePartnerRegistrationFixture.AttendeeEmail).ShouldBeFalse();
            workshopWaitlist.GetActivePosition(UpdatePartnerRegistrationFixture.OtherQueuedEmail).ShouldBe(1);

            var masterclassWaitlist = await dbContext.Waitlists
                .FirstAsync(w => w.Id == fixture.GetTicketTypeId("masterclass"), testContext.CancellationToken);
            masterclassWaitlist.GetActivePosition(UpdatePartnerRegistrationFixture.AttendeeEmail).ShouldBe(1);
        });
    }

    // Given a waitlist coupon that offers a specific ticket type
    // When an update command's confirmed selection omits every ticket type the coupon covers
    // Then a no-coupon-ticket-type-selected error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_CouponTicketTypesNotSelected_ThrowsAndLeavesRegistrationUnchanged()
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
            [],
            new Dictionary<string, string> { ["dietary"] = "vegan" },
            fixture.CouponCode);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(Coupon.Errors.NoCouponTicketTypeSelected([fixture.GetTicketTypeId("workshop").Value]));
        await AssertRegistrationStillOriginal(fixture);
    }

    // Given an organiser coupon whose only ticket type in the submission is one the registration already holds
    // When the attendee updates their registration keeping that ticket, using the coupon
    // Then a no-coupon-ticket-type-selected error is thrown so the coupon is not used up without granting anything
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_CouponOverlapsOnlyExistingTicket_ThrowsAndLeavesRegistrationUnchanged()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithOrganiserMultiTicketTypeCoupon(coverExistingTicket: true);
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value],
            [fixture.GetTicketTypeId("workshop").Value, fixture.GetTicketTypeId("masterclass").Value],
            new Dictionary<string, string> { ["dietary"] = "vegan" },
            fixture.CouponCode);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(Coupon.Errors.NoCouponTicketTypeSelected(
            [fixture.GetTicketTypeId("early-bird").Value, fixture.GetTicketTypeId("workshop").Value]));
        await AssertRegistrationStillOriginal(fixture);
    }

    // Given a partner registration where the workshop ticket type has self-service updates disabled
    // When an update command requests that ticket type
    // Then a ticket-state-conflict error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_SelfServiceDisabledTicket_ThrowsAndLeavesRegistrationUnchanged()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithSelfServiceDisabledWorkshop();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(ValidWorkshopCommand(fixture), testContext.CancellationToken));

        result.Error.ShouldMatch(RegisterAttendeeSelfServiceHandler.Errors.TicketStateConflict(
            new RegisterAttendeeSelfServiceHandler.TicketStateConflict(
                RegisterableTicketTypeIds: [],
                WaitlistableTicketTypeIds: [],
                UnavailableTicketTypeIds: [fixture.GetTicketTypeId("workshop").Value],
                UnknownTicketTypeIds: [],
                InvalidForRequestedActionTicketTypeIds: [])));
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

    // Given a confirmed early-bird ticket and a workshop ticket type genuinely in waitlist mode
    // When the update moves the selection from early-bird to the workshop waitlist
    // Then the early-bird claim is released and an active waitlist entry is added
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_ConfirmedToWaitlist_ReleasesClaimAndAddsWaitlistEntry()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithWaitlistModeWorkshop();
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [],
            [fixture.GetTicketTypeId("workshop").Value],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Tickets.ShouldBeEmpty();
            registration.Status.ShouldBe(RegistrationStatus.Waitlisted);

            var waitlist = await dbContext.Waitlists
                .FirstAsync(w => w.Id == fixture.GetTicketTypeId("workshop"), testContext.CancellationToken);
            waitlist.ActiveEntryCount.ShouldBe(1);
            waitlist.Entries.ShouldContain(e => e.Email == registration.Email);
        });
    }

    // Given a waitlisted workshop ticket type that now has available capacity
    // When the update requests the workshop ticket type as confirmed
    // Then capacity is claimed and the waitlist entry is removed
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_WaitlistToConfirmed_ClaimsCapacityAndRemovesWaitlistEntry()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithAvailableWaitlistEnabledWorkshop(seedWaitlistEntry: true);
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value, fixture.GetTicketTypeId("workshop").Value],
            [],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.Tickets.ShouldContain(t => t.Id == fixture.GetTicketTypeId("workshop"));

            var catalog = await dbContext.TicketCatalogs
                .FirstAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.GetTicketType(fixture.GetTicketTypeId("workshop"))!.PublicUsedCapacity.ShouldBe(1);

            var waitlist = await dbContext.Waitlists
                .FirstAsync(w => w.Id == fixture.GetTicketTypeId("workshop"), testContext.CancellationToken);
            waitlist.ActiveEntryCount.ShouldBe(0);
        });
    }

    // Given a workshop ticket type that is genuinely available, not in waitlist mode
    // When the update requests joining its waitlist instead of registering
    // Then a ticket-state-conflict error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_StaleWaitlistJoinOnAvailableTicket_ThrowsTicketStateConflict()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithAvailableWaitlistEnabledWorkshop();
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value],
            [fixture.GetTicketTypeId("workshop").Value],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(RegisterAttendeeSelfServiceHandler.Errors.TicketStateConflict(
            new RegisterAttendeeSelfServiceHandler.TicketStateConflict(
                RegisterableTicketTypeIds: [fixture.GetTicketTypeId("workshop").Value],
                WaitlistableTicketTypeIds: [],
                UnavailableTicketTypeIds: [],
                UnknownTicketTypeIds: [],
                InvalidForRequestedActionTicketTypeIds: [])));

        var registration = await Environment.RegistrationsDatabase.Context.Registrations
            .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
        registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.GetTicketTypeId("early-bird"));
    }

    // Given a workshop ticket type still genuinely sold out and an existing waitlist entry
    // When the update requests confirming that ticket type instead of staying waitlisted
    // Then a ticket-state-conflict error is thrown and the registration is left unchanged
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_StaleConfirmOnWaitlistModeTicket_ThrowsTicketStateConflict()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithWaitlistModeWorkshop(seedWaitlistEntry: true);
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value, fixture.GetTicketTypeId("workshop").Value],
            [],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

        var result = await ErrorResult.CaptureAsync(
            async () => await CreateSut().HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(RegisterAttendeeSelfServiceHandler.Errors.TicketStateConflict(
            new RegisterAttendeeSelfServiceHandler.TicketStateConflict(
                RegisterableTicketTypeIds: [],
                WaitlistableTicketTypeIds: [fixture.GetTicketTypeId("workshop").Value],
                UnavailableTicketTypeIds: [],
                UnknownTicketTypeIds: [],
                InvalidForRequestedActionTicketTypeIds: [])));

        var registration = await Environment.RegistrationsDatabase.Context.Registrations
            .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
        registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.GetTicketTypeId("early-bird"));
    }

    // Given a confirmed early-bird ticket and an active entry on the sold-out workshop's waitlist
    // When the attendee keeps the early-bird ticket but switches to the sold-out masterclass's waitlist
    // Then a ticket-changed email is sent listing the early-bird ticket as confirmed and the masterclass as waitlisted
    [TestMethod]
    public async ValueTask UpdatePartnerRegistration_WaitlistToWaitlist_SendsTicketChangedEmail()
    {
        var fixture = UpdatePartnerRegistrationFixture.WithTwoWaitlistModeWorkshops();
        await fixture.SetupAsync(Environment);

        var command = new UpdatePartnerRegistrationCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Test",
            [fixture.GetTicketTypeId("early-bird").Value],
            [fixture.GetTicketTypeId("masterclass").Value],
            new Dictionary<string, string> { ["dietary"] = "old" });

        await CreateSut().HandleAsync(command, testContext.CancellationToken);

        var registration = await Environment.RegistrationsDatabase.Context.Registrations
            .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
        var domainEvent = registration.GetDomainEvents()
            .OfType<TicketsChangedDomainEvent>()
            .ShouldHaveSingleItem();

        var delivery = await TicketConfirmationEmailPipeline.PrepareAsync(
            Environment, fixture.TeamId, fixture.EventId, domainEvent, testContext.CancellationToken);

        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        delivery.RecipientAddress.ShouldBe(UpdatePartnerRegistrationFixture.AttendeeEmail.Value);
        var text = delivery.TextBody;
        text.IndexOf("- Early Bird", StringComparison.Ordinal).ShouldBeLessThan(
            text.IndexOf("You're on the waitlist for:", StringComparison.Ordinal));
        text.IndexOf("You're on the waitlist for:", StringComparison.Ordinal).ShouldBeLessThan(
            text.IndexOf("- Masterclass", StringComparison.Ordinal));
        text.ShouldNotContain("- Workshop");
    }

    private static UpdatePartnerRegistrationCommand ValidWorkshopCommand(UpdatePartnerRegistrationFixture fixture) =>
        new(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("workshop").Value],
            [],
            new Dictionary<string, string> { ["dietary"] = "vegan" });

    private static UpdatePartnerRegistrationCommand ValidEarlyBirdCommand(UpdatePartnerRegistrationFixture fixture) =>
        new(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.RegistrationId.Value,
            "Alice",
            "Anderson",
            [fixture.GetTicketTypeId("early-bird").Value],
            [],
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
