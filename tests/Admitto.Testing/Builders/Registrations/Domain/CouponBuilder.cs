using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Testing.Builders.Registrations.Domain;

public class CouponBuilder
{
    public static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    public static readonly EmailAddress DefaultEmail = EmailAddress.From("invitee@example.com");
    public static readonly TicketTypeId DefaultTicketTypeId = TicketTypeId.From(new Guid("11111111-1111-1111-1111-111111111111"));
    public static readonly DateTimeOffset DefaultNow = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset DefaultExpiresAt = new(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private TicketedEventId _eventId = DefaultEventId;
    private EmailAddress _email = DefaultEmail;
    private List<TicketTypeId> _requestedTicketTypeIds = [DefaultTicketTypeId];
    private DateTimeOffset _expiresAt = DefaultExpiresAt;
    private bool _bypassRegistrationWindow;
    private List<TicketTypeInfo> _availableTicketTypes = [new(DefaultTicketTypeId)];
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

    public CouponBuilder WithRequestedTicketTypeIds(params TicketTypeId[] ids)
    {
        _requestedTicketTypeIds = [..ids];
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
            _requestedTicketTypeIds,
            _expiresAt,
            _bypassRegistrationWindow,
            _availableTicketTypes,
            _now);
    }
}
