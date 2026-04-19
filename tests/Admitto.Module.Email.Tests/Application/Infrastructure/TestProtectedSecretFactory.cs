using Amolenk.Admitto.Module.Email.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;

/// <summary>
/// Provides an <see cref="IProtectedSecret"/> backed by ephemeral Data Protection keys for tests.
/// The keyring is in-memory so encrypted blobs are only decryptable within the same test run.
/// </summary>
internal static class TestProtectedSecretFactory
{
    public static IProtectedSecret Create()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("Admitto.Tests");
        services.AddSingleton<IProtectedSecret>(sp =>
        {
            var provider = sp.GetRequiredService<IDataProtectionProvider>();
            var protector = provider.CreateProtector(ProtectedSecretPurpose);
            return new DelegatingProtectedSecret(
                plaintext => protector.Protect(plaintext),
                blob => protector.Unprotect(blob));
        });

        return services.BuildServiceProvider().GetRequiredService<IProtectedSecret>();
    }

    private const string ProtectedSecretPurpose = "Admitto.Email.ConnectionString.v1";

    private sealed class DelegatingProtectedSecret(Func<string, string> protect, Func<string, string> unprotect)
        : IProtectedSecret
    {
        public string Protect(string plaintext) => protect(plaintext);
        public string Unprotect(string protectedValue) => unprotect(protectedValue);
    }
}
