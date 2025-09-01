using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Infrastructure.Persistence.Converters;

public sealed class SecretProtectorConverter(IDataProtectionProvider provider) : ValueConverter<string, string>(
    raw => provider.CreateProtector("Admitto").Protect(raw),
    enc => provider.CreateProtector("Admitto").Unprotect(enc));