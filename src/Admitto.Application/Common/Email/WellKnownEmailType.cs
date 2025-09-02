namespace Amolenk.Admitto.Application.Common.Email;

public static class WellKnownEmailType
{
    public const string Reconfirm = "reconfirm";
    public const string Ticket = "ticket";
    public const string VerifyEmail = "verify-email";

    public static readonly string[] All = [ Reconfirm, Ticket, VerifyEmail ];
}