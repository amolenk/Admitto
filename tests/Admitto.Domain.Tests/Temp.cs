using System.Security.Cryptography;
using System.Text;

namespace Amolenk.Admitto.Domain.Tests;

[TestClass]
public class Temp
{
    [TestMethod]
    public void ComputeHash()
    {
        var codeVerifier = "foo";
        
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var result = Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", ""); // Base64 URL encoding
        
        Console.WriteLine(result);
    }
}