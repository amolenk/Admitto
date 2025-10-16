namespace Amolenk.Admitto.Cli.Commands;

public static class ValidationErrors
{
    public static readonly ValidationResult DataTypeEntityMissing =
        ValidationResult.Error("Data type entity must be specified.");
    
    public static readonly ValidationResult EmailTemplateFolderPathDoesNotExist = 
        ValidationResult.Error("Email template folder path does not exist. Please provide a valid path to the email template file.");

    public static readonly ValidationResult EmailTemplateFolderPathMissing = 
        ValidationResult.Error("Email template folder path must be specified.");
    
    public static readonly ValidationResult EmailMissing = 
        ValidationResult.Error("Email must be specified.");

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
    
    public static readonly ValidationResult IdMissing = 
        ValidationResult.Error("ID must be specified.");
    
    public static readonly ValidationResult RoleMissing = 
        ValidationResult.Error("Role must be specified.");
    
    public static readonly ValidationResult TicketsMissing = 
        ValidationResult.Error("Tickets must be specified.");
    
    public static readonly ValidationResult VerificationTokenMissing = 
        ValidationResult.Error("Verification token must be specified.");

    public static readonly ValidationResult RepeatWindowStartMissing = 
        ValidationResult.Error("Repeat window start must be specified.");

    public static readonly ValidationResult RepeatWindowEndMissing = 
        ValidationResult.Error("Repeat window end must be specified.");

    public static readonly ValidationResult RepeatIntervalMissing = 
        ValidationResult.Error("Repeat interval must be specified.");
    
    public static readonly ValidationResult ReconfirmWindowStartMissing = 
        ValidationResult.Error("Window start must be specified.");

    public static readonly ValidationResult ReconfirmWindowEndMissing = 
        ValidationResult.Error("Window end must be specified.");

    public static readonly ValidationResult ReconfirmInitialDelayMissing = 
        ValidationResult.Error("Initial delay after registration must be specified.");

    public static readonly ValidationResult ReconfirmReminderIntervalDelayMissing = 
        ValidationResult.Error("Reminder interval must be specified.");
    
    public static readonly ValidationResult TeamNameMissing = 
        ValidationResult.Error("Team name must be specified.");
    
    public static readonly ValidationResult TeamSlugMissing = 
        ValidationResult.Error("Team slug must be specified.");
    
    public static readonly ValidationResult EmailServiceConnectionStringMissing = 
        ValidationResult.Error("Email service connection string must be specified.");
    
    public static readonly ValidationResult EventNameMissing = 
        ValidationResult.Error("Event name must be specified.");
    
    public static readonly ValidationResult EventSlugMissing = 
        ValidationResult.Error("Event slug must be specified.");
    
    public static readonly ValidationResult EventWebsiteMissing = 
        ValidationResult.Error("Event website must be specified.");
    
    public static readonly ValidationResult EventBaseUrlMissing = 
        ValidationResult.Error("Event base URL must be specified.");
    
    public static readonly ValidationResult EventStartsAtMissing = 
        ValidationResult.Error("Event start date and time must be specified.");
    
    public static readonly ValidationResult EventEndsAtMissing = 
        ValidationResult.Error("Event end date and time must be specified.");
    
    public static readonly ValidationResult MigrationNameMissing = 
        ValidationResult.Error("Migration name must be specified.");
    
    public static readonly ValidationResult TicketTypeSlugMissing = 
        ValidationResult.Error("Ticket type slug must be specified.");
    
    public static readonly ValidationResult TicketTypeNameMissing = 
        ValidationResult.Error("Ticket type name must be specified.");

    public static readonly ValidationResult TicketTypeMaxCapacityMissing = 
        ValidationResult.Error("Ticket type max capacity must be specified.");

}