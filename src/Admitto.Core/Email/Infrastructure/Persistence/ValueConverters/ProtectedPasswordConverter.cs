using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.ValueConverters;

internal sealed class ProtectedPasswordConverter() : ValueConverter<ProtectedPassword, string>(
    v => v.Ciphertext,
    v => ProtectedPassword.FromCiphertext(v));
