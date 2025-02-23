using System.Security.Cryptography;
using System.Text;

namespace Amolenk.Admitto.Domain.Utilities;

public static class DeterministicGuidGenerator
{
    public static Guid Generate(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        // Take the first 16 bytes to create a GUID.
        // The probability of two different inputs producing the same first 16 bytes is incredibly low.
        return new Guid(hash[..16]);
    }
}