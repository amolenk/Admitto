namespace Amolenk.Admitto.Module.Email.Infrastructure.Security;

/// <summary>
/// Encrypts and decrypts secret strings using a dedicated Data Protection purpose.
/// Decryption is only available inside the Email module's infrastructure.
/// </summary>
public interface IProtectedSecret
{
    /// <summary>Encrypts <paramref name="plaintext"/> for storage at rest.</summary>
    string Protect(string plaintext);

    /// <summary>Decrypts a value previously produced by <see cref="Protect"/>.</summary>
    string Unprotect(string protectedValue);
}
