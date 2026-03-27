using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.ValueConverters;

internal sealed class SlugConverter() : ValueConverter<Slug, string>(
    v => v.Value,
    v => Slug.From(v));
