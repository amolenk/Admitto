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
        public static ApplicationRuleError AlreadyRegistered =>
            new("attendee.already_registered", "A registration for this attendee already exists.");

        public static ApplicationRuleError InvalidVerificationToken =>
            new("attendee.invalid_token", "Verification token is invalid or expired.");
        
        public static ApplicationRuleError InvalidSignature =>
            new("attendee.invalid_signature", "Signature is invalid.");
        
        public static ApplicationRuleError NotFound =>
            new("attendee.not_found", $"Registration not found.");
    }
    
    public static class Signing
    {
        public static ApplicationRuleError InvalidVerificationToken =>
            new("attendee.invalid_token", "Verification token is invalid or expired.");
        
        public static ApplicationRuleError InvalidSignature =>
            new("attendee.invalid_signature", "Signature is invalid.");
    }
    
    
    public static class Contributor
    {
        public static ApplicationRuleError AlreadyExists =>
            new("contributor.already_exists", "A registration for this contributor role already exists.");

        public static ApplicationRuleError NotFound =>
            new("contributor.not_found", "Contributor not found.");
    }
    
    public static class EmailVerificationRequest
    {
        public static ApplicationRuleError Invalid =>
            new("verification_request.invalid", "Email verification request is invalid or expired.");

        public static ApplicationRuleError NotFound(Guid id) =>
            new("verification_request.not_found", $"Email verification request with ID '{id}' does not exist.");
        
        public static readonly ApplicationRuleError VerificationCodeParameterMissing =
            new("verification_request.code_parameter_missing", "Verification code must be passed as an additional parameter.");
    }
    
    public static class General
    {
        public static ApplicationRuleError AlreadyExists =>
            new("general.already_exists", "The item that you tried to create already exists.");
    }
    
    public static class Participant
    {
        public static ApplicationRuleError NotFound =>
            new("participant.not_found", $"Participant not found.");
    }
    
    public static class ContributorRegistration
    {
        public static ApplicationRuleError AlreadyExists =>
            new("contributor.already_exists", "A registration for this attendee already exists.");
        
        public static ApplicationRuleError NotFound =>
            new("contributor.not_found", $"Registration not found.");
    }
    
    public static class Team
    {
        public static ApplicationRuleError NotFound =>
            new("team.not_found", "Team does not exist.");
    }
    
    public static class TicketedEvent
    {
        public static ApplicationRuleError NotFound =>
            new("event.not_found", $"Ticketed event does not exist.");
    }
    
}