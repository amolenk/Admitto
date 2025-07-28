using System.Security.Cryptography;

namespace Amolenk.Admitto.Domain.ValueObjects;

public record EmailVerification(string Code, DateTime ExpirationTime)
{
    public bool IsExpired => DateTime.UtcNow > ExpirationTime;
    
    public static EmailVerification Generate()
    {
        return new EmailVerification(
            GenerateConfirmationCode(),
            DateTime.UtcNow.AddMinutes(15));
    }
    
    private static string GenerateConfirmationCode()
    {
        // Generates a random 6-digit code (000000-999999)
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
    
        var value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // Ensure non-negative
        var code = value % 1_000_000;
    
        return code.ToString("D6");
    }
}