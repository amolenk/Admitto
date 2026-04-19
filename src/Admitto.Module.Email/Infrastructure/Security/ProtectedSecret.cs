using Microsoft.AspNetCore.DataProtection;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Security;

internal sealed class ProtectedSecret : IProtectedSecret
{
    /// <summary>
    /// Purpose string scoped to the Email module so cross-purpose decryption attempts fail.
    /// </summary>
    public const string Purpose = "Admitto.Email.ConnectionString.v1";

    private readonly IDataProtector _protector;

    public ProtectedSecret(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
