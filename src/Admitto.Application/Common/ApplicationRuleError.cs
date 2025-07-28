namespace Amolenk.Admitto.Application.Common;

/// <summary>
/// Represents a business rule error in the application layer.
/// </summary>
public sealed class ApplicationRuleError
{
    private ApplicationRuleError(string code, string messageText)
    {
        Code = code;
        MessageText = messageText;
    }

    public string Code { get; }
    public string MessageText { get; }
    
    public override string ToString() => $"{Code}: {MessageText}";

    public static class Attendee
    {
        public static ApplicationRuleError NotFound(Guid id) =>
            new("attendee.not_found", $"Attendee with ID '{id}' does not exist.");
    }

    public static class EmailVerificationRequest
    {
        public static ApplicationRuleError Invalid =
            new("verification_request.invalid", "Email verification request is invalid or expired.");

        public static ApplicationRuleError NotFound(Guid id) =>
            new("verification_request.not_found", $"Email verification request with ID '{id}' does not exist.");
        
        public static readonly ApplicationRuleError VerificationCodeParameterMissing =
            new("verification_request.code_parameter_missing", "Verification code must be passed as an additional parameter.");
    }
}