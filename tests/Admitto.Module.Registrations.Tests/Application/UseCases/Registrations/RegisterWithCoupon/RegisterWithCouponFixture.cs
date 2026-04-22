using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterWithCoupon;

// ReSharper disable once UnusedType.Global
internal sealed class RegisterWithCouponFixture
{
    private Coupon? _coupon;
    private TicketedEvent? _ticketedEvent;
    private TicketCatalog? _catalog;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public string TicketTypeSlug { get; } = "speaker-pass";
    public string CouponCodeString { get; private set; } = string.Empty;
    public IOrganizationFacade OrganizationFacade { get; } = Substitute.For<IOrganizationFacade>();

    private RegisterWithCouponFixture()
    {
    }

    // ── Factory methods ──────────────────────────────────────────────────────

    public static RegisterWithCouponFixture HappyFlow()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
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
            .WithExpiresAt(DateTimeOffset.UtcNow.AddMinutes(-1))
            .Build();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterWithCouponFixture RedeemedCoupon()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon();
        f._coupon.Redeem();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterWithCouponFixture RevokedCoupon()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon();
        f._coupon.Revoke();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        return f;
    }

    public static RegisterWithCouponFixture TicketTypeNotAllowlisted()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon();
        f.CouponCodeString = f._coupon.Code.Value.ToString();
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog(
            ("general-admission", "General Admission", 100, 0),
            (f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    public static RegisterWithCouponFixture BypassesNullCapacity()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: false);
        f._ticketedEvent = f.MakeActiveEventWithOpenWindow();
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", null, 0));
        return f;
    }

    public static RegisterWithCouponFixture BypassesClosedWindow()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: true);
        var closedPolicy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddDays(-1));
        f._ticketedEvent = f.MakeActiveEvent(closedPolicy);
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    public static RegisterWithCouponFixture RespectsClosedWindow()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: false);
        var closedPolicy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddDays(-1));
        f._ticketedEvent = f.MakeActiveEvent(closedPolicy);
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    public static RegisterWithCouponFixture BypassesDomainRestriction()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: false);
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            "@acme.com");
        f._ticketedEvent = f.MakeActiveEvent(policy);
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    public static RegisterWithCouponFixture EventCancelled()
    {
        var f = new RegisterWithCouponFixture();
        f._coupon = f.BuildCoupon(bypassWindow: true);
        var ev = f.MakeActiveEventWithOpenWindow();
        ev.Cancel();
        f._ticketedEvent = ev;
        f._catalog = f.MakeCatalog((f.TicketTypeSlug, "Speaker Pass", 100, 0));
        return f;
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_coupon is null && _catalog is null && _ticketedEvent is null) return;

        await environment.Database.SeedAsync(dbContext =>
        {
            if (_coupon is not null)
                dbContext.Coupons.Add(_coupon);
            if (_ticketedEvent is not null)
                dbContext.TicketedEvents.Add(_ticketedEvent);
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

    private TicketedEvent MakeActiveEventWithOpenWindow()
    {
        var policy = TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));
        return MakeActiveEvent(policy);
    }

    private TicketedEvent MakeActiveEvent(TicketedEventRegistrationPolicy? policy)
    {
        var ev = TicketedEvent.Create(
            EventId,
            TeamId,
            Slug.From("devconf"),
            DisplayName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61));
        if (policy is not null)
            ev.ConfigureRegistrationPolicy(policy);
        return ev;
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
