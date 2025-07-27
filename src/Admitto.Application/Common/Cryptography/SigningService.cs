using System.Security.Cryptography;
using System.Text;

namespace Amolenk.Admitto.Application.Common.Cryptography;

public interface ISigningService
{
    string Sign(Guid value);
    string Sign(string value);
    bool IsValid(Guid value, string signature);
    bool IsValid(string value, string signature);
}

public class SigningService(IConfiguration configuration) : ISigningService
{
    private readonly string _secretKey =
        configuration["Signing:SecretKey"] ?? throw new ArgumentException("Signing key not configured");

    public string Sign(Guid value) => Sign(value.ToString());

    public string Sign(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
        // Ensure the signature is URL-safe
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public bool IsValid(Guid value, string signature) => IsValid(value.ToString(), signature);
    
    public bool IsValid(string value, string signature)
    {
        var expectedSignature = Sign(value);
        return TimingSafeEquals(expectedSignature, signature);
    }
    
    /// <summary>
    /// Prevents timing attacks by comparing two strings in constant time.
    /// </summary>
    private static bool TimingSafeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        return aBytes.Length == bBytes.Length 
               && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}