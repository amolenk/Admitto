namespace Amolenk.Admitto.Cli.Commands;

public static class ValidationErrors
{
    public static readonly ValidationResult DataTypeEntityMissing =
        ValidationResult.Error("Data type entity must be specified.");
    
    public static readonly ValidationResult EmailBodyPathDoesNotExist = 
        ValidationResult.Error("Email body path does not exist. Please provide a valid path to the email body template file.");

    public static readonly ValidationResult EmailBodyPathMissing = 
        ValidationResult.Error("Email body path must be specified.");
    
    public static readonly ValidationResult EmailMissing = 
        ValidationResult.Error("Email must be specified.");

    public static readonly ValidationResult EmailSubjectMissing = 
        ValidationResult.Error("Email subject must be specified.");

    public static readonly ValidationResult EmailRecipientMissing =
        ValidationResult.Error("Email recipient must be specified.");
    
    public static readonly ValidationResult EmailTypeMissing = 
        ValidationResult.Error("Email type must be specified.");
    
    public static readonly ValidationResult EmailVerificationCodeMissing = 
        ValidationResult.Error("Verification code must be specified.");
    
    public static readonly ValidationResult FirstNameMissing = 
        ValidationResult.Error("First name must be specified.");

    public static readonly ValidationResult LastNameMissing = 
        ValidationResult.Error("Last name must be specified.");
    
    public static readonly ValidationResult RegistrationIdMissing = 
        ValidationResult.Error("Registration ID must be specified.");

    public static readonly ValidationResult TicketsMissing = 
        ValidationResult.Error("Tickets must be specified.");
    
    public static readonly ValidationResult VerificationTokenMissing = 
        ValidationResult.Error("Verification token must be specified.");
}