using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterWithCoupon;

internal sealed class RegisterWithCouponFixture
{
    private Coupon? _coupon;
    private EventRegistrationPolicy? _policy;
    private EventCapacity? _eventCapacity;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public string TicketTypeSlug { get; } = "speaker-pass";
    public string CouponCodeString { get; private set; } = string.Empty;
    public IOrganizationFacade OrganizationFacade { get; } = Substitute.For<IOrganizationFacade>();

    private RegisterWithCouponFixture()
    {
    }

    // ── Factory methods ──────────────────────────────────────────────────────

    /// Successful coupon registration: capacity exceeded (5/5) but coupon bypasses it.
    public static RegisterWithCouponFixture HappyFlow()
    {
        var f = new RegisterWithCouponFixture();
        var ticketTypes = new List<TicketTypeDto>
        {
            new() { Slug = f.TicketTypeSlug, Name = "Speaker Pass", IsCancelled = false }
        };
        f.SetupFacade(ticketTypes, eventActive: true);

        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._policy = f.MakeOpenPolicy();
        // Capacity at 5/5 — coupon should still succeed (uncapped).
        f._eventCapacity = f.MakeCapacity(f.TicketTypeSlug, 5, 5);
        return f;
    }

    public static RegisterWithCouponFixture ExpiredCoupon()
    {
        var f = new RegisterWithCouponFixture();
        f.SetupFacade(
        [
            new TicketTypeDto { Slug = f.TicketTypeSlug, Name = "Speaker Pass", IsCancelled = false }
        ], eventActive: true);

        f._coupon = new CouponBuilder()
            .WithEventId(f.EventId)
            .WithRequestedTicketTypeSlugs(f.TicketTypeSlug)
            .WithAvailableTicketTypes(new TicketTypeInfo(f.TicketTypeSlug, false))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddMinutes(-1)) // expired
            .Build();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._policy = f.MakeOpenPolicy();
        return f;
    }

    public static RegisterWithCouponFixture RedeemedCoupon()
    {
        var f = new RegisterWithCouponFixture();
        f.SetupFacade(
        [
            new TicketTypeDto { Slug = f.TicketTypeSlug, Name = "Speaker Pass", IsCancelled = false }
        ], eventActive: true);

        f._coupon = f.BuildCoupon();
        f._coupon.Redeem();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._policy = f.MakeOpenPolicy();
        return f;
    }

    public static RegisterWithCouponFixture RevokedCoupon()
    {
        var f = new RegisterWithCouponFixture();
        f.SetupFacade(
        [
            new TicketTypeDto { Slug = f.TicketTypeSlug, Name = "Speaker Pass", IsCancelled = false }
        ], eventActive: true);

        f._coupon = f.BuildCoupon();
        f._coupon.Revoke();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._policy = f.MakeOpenPolicy();
        return f;
    }

    public static RegisterWithCouponFixture TicketTypeNotAllowlisted()
    {
        var f = new RegisterWithCouponFixture();
        f.SetupFacade(
        [
            new TicketTypeDto { Slug = "general-admission", Name = "General Admission", IsCancelled = false },
            new TicketTypeDto { Slug = f.TicketTypeSlug, Name = "Speaker Pass", IsCancelled = false }
        ], eventActive: true);

        // Coupon only allows "speaker-pass", but test will try "general-admission".
        f._coupon = f.BuildCoupon();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._policy = f.MakeOpenPolicy();
        return f;
    }

    public static RegisterWithCouponFixture BypassesWindow()
    {
        var f = new RegisterWithCouponFixture();
        f.SetupFacade(
        [
            new TicketTypeDto { Slug = f.TicketTypeSlug, Name = "Speaker Pass", IsCancelled = false }
        ], eventActive: true);

        f._coupon = f.BuildCoupon(bypassWindow: true);
        // Window is closed.
        f._policy = EventRegistrationPolicy.Create(f.EventId);
        f._policy.SetWindow(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-1));
        f._eventCapacity = f.MakeCapacity(f.TicketTypeSlug, 10, 0);
        return f;
    }

    public static RegisterWithCouponFixture RespectsWindow()
    {
        var f = new RegisterWithCouponFixture();
        f.SetupFacade(
        [
            new TicketTypeDto { Slug = f.TicketTypeSlug, Name = "Speaker Pass", IsCancelled = false }
        ], eventActive: true);

        f._coupon = f.BuildCoupon(bypassWindow: false);
        // Window is closed.
        f._policy = EventRegistrationPolicy.Create(f.EventId);
        f._policy.SetWindow(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-1));
        f._eventCapacity = f.MakeCapacity(f.TicketTypeSlug, 10, 0);
        return f;
    }

    public static RegisterWithCouponFixture BypassesDomainRestriction()
    {
        var f = new RegisterWithCouponFixture();
        f.SetupFacade(
        [
            new TicketTypeDto { Slug = f.TicketTypeSlug, Name = "Speaker Pass", IsCancelled = false }
        ], eventActive: true);

        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._policy = f.MakeOpenPolicy();
        f._policy.SetDomainRestriction("@acme.com");
        f._eventCapacity = f.MakeCapacity(f.TicketTypeSlug, 10, 0);
        return f;
    }

    public static RegisterWithCouponFixture BypassesNullCapacity()
    {
        var f = new RegisterWithCouponFixture();
        f.SetupFacade(
        [
            new TicketTypeDto { Slug = f.TicketTypeSlug, Name = "Speaker Pass", IsCancelled = false }
        ], eventActive: true);

        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._policy = f.MakeOpenPolicy();
        // MaxCapacity = null — self-service would reject this but coupon should bypass.
        f._eventCapacity = f.MakeCapacity(f.TicketTypeSlug, null, 0);
        return f;
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_coupon is null && _policy is null && _eventCapacity is null) return;

        await environment.Database.SeedAsync(dbContext =>
        {
            if (_coupon is not null)
                dbContext.Coupons.Add(_coupon);
            if (_policy is not null)
                dbContext.EventRegistrationPolicies.Add(_policy);
            if (_eventCapacity is not null)
                dbContext.EventCapacities.Add(_eventCapacity);
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Coupon BuildCoupon(bool bypassWindow = false)
    {
        var coupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithRequestedTicketTypeSlugs(TicketTypeSlug)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeSlug, false))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .WithBypassRegistrationWindow(bypassWindow)
            .Build();
        CouponCodeString = coupon.Code.Value.ToString();
        return coupon;
    }

    private void SetupFacade(List<TicketTypeDto> ticketTypes, bool eventActive)
    {
        OrganizationFacade
            .GetTicketTypesAsync(EventId.Value, Arg.Any<CancellationToken>())
            .Returns(ticketTypes.ToArray());
        OrganizationFacade
            .IsEventActiveAsync(EventId.Value, Arg.Any<CancellationToken>())
            .Returns(eventActive);
    }

    private EventRegistrationPolicy MakeOpenPolicy()
    {
        var policy = EventRegistrationPolicy.Create(EventId);
        policy.SetWindow(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));
        return policy;
    }

    private EventCapacity MakeCapacity(string slug, int? max, int used)
    {
        var capacity = EventCapacity.Create(EventId);
        capacity.SetTicketCapacity(slug, max);
        if (used > 0)
        {
            for (var i = 0; i < used; i++)
                capacity.Claim([slug], enforce: false);
        }

        return capacity;
    }
}
