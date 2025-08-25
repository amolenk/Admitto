namespace Amolenk.Admitto.Application.Common.Email;

public static class EmailExtensions
{
    public static string NormalizeEmail(this string email) =>
        email.Trim().ToLowerInvariant();
}