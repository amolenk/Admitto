using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.ValueConverters;

internal sealed class DisplayNameConverter() : ValueConverter<DisplayName, string>(
    v => v.Value,
    v => DisplayName.From(v));
