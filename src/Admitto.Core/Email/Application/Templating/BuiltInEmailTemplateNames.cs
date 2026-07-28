namespace Amolenk.Admitto.Core.Email.Application.Templating;

/// <summary>
/// Well-known, reserved names for built-in email templates.
/// These constants are the stable lookup keys used by runtime code
/// (event handlers, jobs) to identify which template to render.
/// </summary>
public static class BuiltInEmailTemplateNames
{
    public const string TicketConfirmation = "Ticket confirmation";
    public const string Reconfirmation     = "Reconfirmation";
    public const string Cancellation       = "Cancellation";
    public const string ReconfirmCancelled = "Reconfirm cancelled";
    public const string VisaLetterDenied   = "Visa letter denied";
    public const string VerificationCode   = "Verification code";
    public const string CouponInvitation   = "Coupon invitation";
    public const string WaitlistNotification = "Waitlist notification";
    public const string BulkCustom = "bulk-custom";
}
