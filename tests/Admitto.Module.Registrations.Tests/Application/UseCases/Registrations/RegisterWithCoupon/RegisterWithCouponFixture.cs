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
    private TicketCatalog? _catalog;

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
        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._policy = f.MakeOpenPolicy();
        // Capacity at 5/5 — coupon should still succeed (uncapped).
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 5, 5));
        return f;
    }

    public static RegisterWithCouponFixture ExpiredCoupon()
    {
        var f = new RegisterWithCouponFixture();
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
        f._coupon = f.BuildCoupon();
        f._coupon.Redeem();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._policy = f.MakeOpenPolicy();
        return f;
    }

    public static RegisterWithCouponFixture RevokedCoupon()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon();
        f._coupon.Revoke();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._policy = f.MakeOpenPolicy();
        return f;
    }

    public static RegisterWithCouponFixture TicketTypeNotAllowlisted()
    {
        var f = new RegisterWithCouponFixture();
        // Coupon only allows "speaker-pass", but test will try "general-admission".
        f._coupon = f.BuildCoupon();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._policy = f.MakeOpenPolicy();
        f._catalog = f.MakeCatalog(
            ("general-admission", "General Admission", 100, 0),
            (f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    public static RegisterWithCouponFixture BypassesWindow()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: true);
        // Window is closed.
        f._policy = EventRegistrationPolicy.Create(f.EventId);
        f._policy.SetWindow(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-1));
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 10, 0));
        return f;
    }

    public static RegisterWithCouponFixture RespectsWindow()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: false);
        // Window is closed.
        f._policy = EventRegistrationPolicy.Create(f.EventId);
        f._policy.SetWindow(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-1));
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 10, 0));
        return f;
    }

    public static RegisterWithCouponFixture BypassesDomainRestriction()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._policy = f.MakeOpenPolicy();
        f._policy.SetDomainRestriction("@acme.com");
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 10, 0));
        return f;
    }

    public static RegisterWithCouponFixture BypassesNullCapacity()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._policy = f.MakeOpenPolicy();
        // MaxCapacity = null — self-service would reject this but coupon should bypass.
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", null, 0));
        return f;
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_coupon is null && _policy is null && _catalog is null) return;

        await environment.Database.SeedAsync(dbContext =>
        {
            if (_coupon is not null)
                dbContext.Coupons.Add(_coupon);
            if (_policy is not null)
                dbContext.EventRegistrationPolicies.Add(_policy);
            if (_catalog is not null)
                dbContext.TicketCatalogs.Add(_catalog);
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

    private EventRegistrationPolicy MakeOpenPolicy()
    {
        var policy = EventRegistrationPolicy.Create(EventId);
        policy.SetWindow(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));
        return policy;
    }

    private TicketCatalog MakeCatalog(params (string slug, string name, int? max, int used)[] ticketTypes)
    {
        var catalog = TicketCatalog.Create(EventId);
        foreach (var (slug, name, max, used) in ticketTypes)
        {
            catalog.AddTicketType(Slug.From(slug), DisplayName.From(name), [], max);
            if (used > 0)
            {
                for (var i = 0; i < used; i++)
                    catalog.Claim([slug], enforce: false);
            }
        }
        return catalog;
    }
}
