namespace Amolenk.Admitto.Application.Jobs;

public static class WellKnownJob
{
    public const string SendBulkEmails = "send-bulk-emails";

    public static readonly string[] All = [SendBulkEmails];
}