namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// Provides standard maximum lengths for various common string columns in the database.
/// </summary>
public static class ColumnMaxLength
{
    public const int EmailAddress = 254;
    public const int EmailStatus = 10;
    public const int EmailSubject = 255;
    public const int EmailType = 50;
    public const int FirstName = 100;
    public const int LastName = 150;
    public const int Slug = 50;
    public const int TeamName = 50;
    public const int TicketedEventName = 50;
    public const int Url = 255;
}