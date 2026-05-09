using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.ValueConverters;

internal sealed class EmailAddressConverter() : ValueConverter<EmailAddress, string>(
    v => v.Value,
    v => EmailAddress.From(v));
