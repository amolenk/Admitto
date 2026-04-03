using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Builders;

public class CouponBuilder
{
    public static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    public static readonly EmailAddress DefaultEmail = EmailAddress.From("invitee@example.com");
    public static readonly string DefaultTicketTypeSlug = "general-admission";
    public static readonly DateTimeOffset DefaultNow = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset DefaultExpiresAt = new(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private TicketedEventId _eventId = DefaultEventId;
    private EmailAddress _email = DefaultEmail;
    private List<string> _requestedTicketTypeSlugs = [DefaultTicketTypeSlug];
    private DateTimeOffset _expiresAt = DefaultExpiresAt;
    private bool _bypassRegistrationWindow;
    private List<TicketTypeInfo> _availableTicketTypes = [new(DefaultTicketTypeSlug, IsCancelled: false)];
    private DateTimeOffset _now = DefaultNow;

    public CouponBuilder WithEventId(TicketedEventId eventId)
    {
        _eventId = eventId;
        return this;
    }

    public CouponBuilder WithEmail(EmailAddress email)
    {
        _email = email;
        return this;
    }

    public CouponBuilder WithRequestedTicketTypeSlugs(params string[] slugs)
    {
        _requestedTicketTypeSlugs = [..slugs];
        return this;
    }

    public CouponBuilder WithAvailableTicketTypes(params TicketTypeInfo[] ticketTypes)
    {
        _availableTicketTypes = [..ticketTypes];
        return this;
    }

    public CouponBuilder WithExpiresAt(DateTimeOffset expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    public CouponBuilder WithBypassRegistrationWindow(bool bypass = true)
    {
        _bypassRegistrationWindow = bypass;
        return this;
    }

    public CouponBuilder WithNow(DateTimeOffset now)
    {
        _now = now;
        return this;
    }

    public Coupon Build()
    {
        return Coupon.Create(
            _eventId,
            _email,
            _requestedTicketTypeSlugs,
            _expiresAt,
            _bypassRegistrationWindow,
            _availableTicketTypes,
            _now);
    }
}
