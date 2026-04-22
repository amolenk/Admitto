using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

/// <summary>
/// Value-object policy describing when self-service registration is open for a
/// <c>TicketedEvent</c> and (optionally) which email domain is permitted.
/// </summary>
public sealed record TicketedEventRegistrationPolicy
{
    public DateTimeOffset OpensAt { get; }
    public DateTimeOffset ClosesAt { get; }
    public string? AllowedEmailDomain { get; }

    private TicketedEventRegistrationPolicy(
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        string? allowedEmailDomain)
    {
        OpensAt = opensAt;
        ClosesAt = closesAt;
        AllowedEmailDomain = allowedEmailDomain;
    }

    public static TicketedEventRegistrationPolicy Create(
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        string? allowedEmailDomain = null)
    {
        if (closesAt <= opensAt)
            throw new BusinessRuleViolationException(Errors.WindowCloseBeforeOpen);

        return new TicketedEventRegistrationPolicy(opensAt, closesAt, allowedEmailDomain);
    }

    public bool IsWithinWindow(DateTimeOffset now) =>
        now >= OpensAt && now < ClosesAt;

    public bool IsEmailDomainAllowed(string email)
    {
        if (AllowedEmailDomain is null) return true;
        return email.EndsWith(AllowedEmailDomain, StringComparison.OrdinalIgnoreCase);
    }

    internal static class Errors
    {
        public static readonly Error WindowCloseBeforeOpen = new(
            "ticketed_event_registration_policy.window_close_before_open",
            "Registration window close time must be strictly after open time.");
    }
}
