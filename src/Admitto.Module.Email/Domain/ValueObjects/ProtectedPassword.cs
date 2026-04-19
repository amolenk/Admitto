namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

/// <summary>
/// Opaque marker for an encrypted password ciphertext produced by
/// <c>IProtectedSecret.Protect</c>. The type guarantees that the wrapped string has been through
/// the encryption boundary; no format check is performed.
/// </summary>
/// <remarks>
/// Construction is module-internal so callers cannot accidentally wrap plaintext. Tests gain
/// access via the assembly-level <c>InternalsVisibleTo</c> entries in
/// <c>Directory.Build.props</c>.
/// </remarks>
public readonly record struct ProtectedPassword
{
    public string Ciphertext { get; }

    private ProtectedPassword(string ciphertext) => Ciphertext = ciphertext;

    /// <summary>
    /// Wraps a string already produced by <c>IProtectedSecret.Protect(...)</c>. Internal so the
    /// encryption boundary cannot be bypassed from outside the module.
    /// </summary>
    internal static ProtectedPassword FromCiphertext(string ciphertext) => new(ciphertext);

    public override string ToString() => "<protected>";
}
