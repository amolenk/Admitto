using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.ValueConverters;

internal sealed class DisplayNameConverter() : ValueConverter<DisplayName, string>(
    v => v.Value,
    v => DisplayName.From(v));
