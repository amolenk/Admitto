using Microsoft.AspNetCore.DataProtection;

namespace Amolenk.Admitto.Application.Common.Cryptography;

// TODO IEncryptionService for global use

public interface ITeamConfigEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedData);
}

public class TeamConfigEncryptionService(IDataProtectionProvider provider) : ITeamConfigEncryptionService
{
    private readonly IDataProtector _protector = provider.CreateProtector("TeamConfig");

    public string Encrypt(string plainText) => _protector.Protect(plainText);

    public string Decrypt(string encryptedData) => _protector.Unprotect(encryptedData);
}