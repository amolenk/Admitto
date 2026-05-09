using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Core.Module.Email.Infrastructure.Persistence.ValueConverters;

internal sealed class ProtectedPasswordConverter() : ValueConverter<ProtectedPassword, string>(
    v => v.Ciphertext,
    v => ProtectedPassword.FromCiphertext(v));
