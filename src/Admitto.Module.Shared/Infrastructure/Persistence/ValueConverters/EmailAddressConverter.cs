using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.ValueConverters;

internal sealed class EmailAddressConverter() : ValueConverter<EmailAddress, string>(
    v => v.Value,
    v => EmailAddress.From(v));
